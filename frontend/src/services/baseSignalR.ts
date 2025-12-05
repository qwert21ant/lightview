import * as signalR from '@microsoft/signalr';
import { ref } from 'vue';
import { authService } from './auth';
import { API_BASE_URL } from '@/config/api';

export abstract class BaseSignalRService {
  protected connection: signalR.HubConnection | null = null;
  protected maxReconnectAttempts = 5;
  protected abstract hubPath: string;

  // Reactive state - shared across all SignalR services
  public connectionState = ref<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);
  public isConnecting = ref(false);
  public connectionError = ref<string | null>(null);

  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    if (this.isConnecting.value) {
      return;
    }

    this.isConnecting.value = true;
    this.connectionError.value = null;

    try {
      const token = authService.getToken();
      if (!token) {
        throw new Error('No authentication token available');
      }

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}${this.hubPath}`, {
          accessTokenFactory: () => token,
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
            return null; // Stop reconnecting
          },
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Set up common connection event handlers
      this.setupConnectionEventHandlers();

      // Allow derived classes to set up their specific event handlers
      this.setupEventHandlers();

      await this.connection.start();
      this.connectionState.value = this.connection.state;

      console.log(`SignalR: Connected to ${this.hubPath}`);

      // Allow derived classes to perform post-connection initialization
      await this.onConnected();
    } catch (error) {
      console.error(`Failed to connect to SignalR hub ${this.hubPath}:`, error)
      
      // Handle authentication errors
      if (error instanceof Error && 
          (error.message.includes('401') || 
           error.message.includes('Unauthorized') ||
           error.message.includes('Authentication failed'))) {
        console.warn('SignalR authentication failed - redirecting to login')
        authService.logout()
        return
      }
      
      this.connectionError.value = error instanceof Error ? error.message : 'Connection failed'
      this.connectionState.value = signalR.HubConnectionState.Disconnected
      throw error
    } finally {
      this.isConnecting.value = false
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.connectionState.value = signalR.HubConnectionState.Disconnected;
      console.log(`SignalR: Disconnected from ${this.hubPath}`);
    }
  }

  private setupConnectionEventHandlers(): void {
    if (!this.connection) return;

    this.connection.onreconnecting(() => {
      this.connectionState.value = signalR.HubConnectionState.Reconnecting;
      console.log(`SignalR: Reconnecting to ${this.hubPath}...`);
      this.onReconnecting();
    });

    this.connection.onreconnected(() => {
      this.connectionState.value = signalR.HubConnectionState.Connected;
      console.log(`SignalR: Reconnected successfully to ${this.hubPath}`);
      this.onReconnected();
    });

    this.connection.onclose((error) => {
      this.connectionState.value = signalR.HubConnectionState.Disconnected;
      if (error) {
        console.error(`SignalR connection to ${this.hubPath} closed with error:`, error);
        this.connectionError.value = error.message;
      } else {
        console.log(`SignalR connection to ${this.hubPath} closed`);
      }
      this.onClosed(error);
    });
  }

  // Helper methods
  protected ensureConnected(): void {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(`SignalR connection to ${this.hubPath} is not established. Call connect() first.`);
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  public getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  // Protected methods for invoking hub methods
  protected async invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
    this.ensureConnected()
    try {
      return await this.connection!.invoke(methodName, ...args) as T
    } catch (error) {
      console.error(`Failed to invoke ${methodName} on ${this.hubPath}:`, error)
      
      // Handle authentication errors
      if (error instanceof Error && 
          (error.message.includes('401') || 
           error.message.includes('Unauthorized') ||
           error.message.includes('Authentication failed'))) {
        console.warn('SignalR method invocation failed due to authentication - redirecting to login')
        authService.logout()
        return Promise.reject(new Error('Authentication required'))
      }
      
      throw error
    }
  }

  protected on(methodName: string, handler: (...args: any[]) => void): void {
    if (this.connection) {
      this.connection.on(methodName, handler);
    }
  }

  protected off(methodName: string, handler?: (...args: any[]) => void): void {
    if (this.connection) {
      if (handler) {
        this.connection.off(methodName, handler);
      } else {
        this.connection.off(methodName);
      }
    }
  }

  // Abstract methods that derived classes must implement
  protected abstract setupEventHandlers(): void;

  // Virtual methods that derived classes can override
  protected async onConnected(): Promise<void> {
    // Default implementation: do nothing
  }

  protected onReconnecting(): void {
    // Default implementation: do nothing
  }

  protected onReconnected(): void {
    // Default implementation: do nothing
  }

  protected onClosed(error?: Error): void {
    // Default implementation: do nothing
  }
}