using System.Text.Json;
using Lightview.Shared.Contracts;
using PersistenceCamera = Persistence.Models.Camera;
using SharedCamera = Lightview.Shared.Contracts.Camera;

namespace CameraManager.Mappers;

public static class CameraMapper
{
    public static SharedCamera ToSharedCamera(this PersistenceCamera persistenceCamera)
    {
        return new SharedCamera
        {
            Id = persistenceCamera.Id,
            Name = persistenceCamera.Name,
            Url = new Uri(persistenceCamera.Url),
            Username = persistenceCamera.Username,
            Password = persistenceCamera.Password,
            Protocol = (CameraProtocol)persistenceCamera.Protocol,
            Status = (CameraStatus)persistenceCamera.Status,
            CreatedAt = persistenceCamera.CreatedAt,
            LastConnectedAt = persistenceCamera.LastConnectedAt,
            Capabilities = DeserializeCapabilities(persistenceCamera.CapabilitiesJson),
            Profiles = DeserializeProfiles(persistenceCamera.ProfilesJson),
            DeviceInfo = DeserializeDeviceInfo(persistenceCamera.DeviceInfoJson)
        };
    }

    public static PersistenceCamera ToPersistenceCamera(this SharedCamera sharedCamera)
    {
        return new PersistenceCamera
        {
            Id = sharedCamera.Id,
            Name = sharedCamera.Name,
            Url = sharedCamera.Url.ToString(),
            Username = sharedCamera.Username,
            Password = sharedCamera.Password,
            Protocol = (int)sharedCamera.Protocol,
            Status = (int)sharedCamera.Status,
            CreatedAt = sharedCamera.CreatedAt,
            LastConnectedAt = sharedCamera.LastConnectedAt,
            CapabilitiesJson = SerializeCapabilities(sharedCamera.Capabilities),
            ProfilesJson = SerializeProfiles(sharedCamera.Profiles),
            DeviceInfoJson = SerializeDeviceInfo(sharedCamera.DeviceInfo)
        };
    }

    public static void UpdatePersistenceCamera(this PersistenceCamera persistenceCamera, SharedCamera sharedCamera)
    {
        persistenceCamera.Name = sharedCamera.Name;
        persistenceCamera.Url = sharedCamera.Url.ToString();
        persistenceCamera.Username = sharedCamera.Username;
        persistenceCamera.Password = sharedCamera.Password;
        persistenceCamera.Protocol = (int)sharedCamera.Protocol;
        persistenceCamera.Status = (int)sharedCamera.Status;
        persistenceCamera.LastConnectedAt = sharedCamera.LastConnectedAt;
        persistenceCamera.CapabilitiesJson = SerializeCapabilities(sharedCamera.Capabilities);
        persistenceCamera.ProfilesJson = SerializeProfiles(sharedCamera.Profiles);
        persistenceCamera.DeviceInfoJson = SerializeDeviceInfo(sharedCamera.DeviceInfo);
    }

    private static string? SerializeCapabilities(CameraCapabilities? capabilities)
    {
        return capabilities != null ? JsonSerializer.Serialize(capabilities) : null;
    }

    private static CameraCapabilities? DeserializeCapabilities(string? capabilitiesJson)
    {
        return !string.IsNullOrEmpty(capabilitiesJson) 
            ? JsonSerializer.Deserialize<CameraCapabilities>(capabilitiesJson) 
            : null;
    }

    private static string? SerializeProfiles(List<CameraProfile> profiles)
    {
        return profiles?.Count > 0 ? JsonSerializer.Serialize(profiles) : null;
    }

    private static List<CameraProfile> DeserializeProfiles(string? profilesJson)
    {
        return !string.IsNullOrEmpty(profilesJson) 
            ? JsonSerializer.Deserialize<List<CameraProfile>>(profilesJson) ?? new()
            : new();
    }

    private static string? SerializeDeviceInfo(CameraDeviceInfo? deviceInfo)
    {
        return deviceInfo != null ? JsonSerializer.Serialize(deviceInfo) : null;
    }

    private static CameraDeviceInfo? DeserializeDeviceInfo(string? deviceInfoJson)
    {
        return !string.IsNullOrEmpty(deviceInfoJson) 
            ? JsonSerializer.Deserialize<CameraDeviceInfo>(deviceInfoJson) 
            : null;
    }
}