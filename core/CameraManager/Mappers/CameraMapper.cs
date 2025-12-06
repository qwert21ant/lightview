using System.Text.Json;
using Lightview.Shared.Contracts;
using PersistenceCamera = Persistence.Models.Camera;
using PersistenceCameraMetadata = Persistence.Models.CameraMetadata;
using PersistenceCameraProfile = Persistence.Models.CameraProfile;
using SharedCamera = Lightview.Shared.Contracts.Camera;
using SharedCameraProfile = Lightview.Shared.Contracts.CameraProfile;

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
            CreatedAt = persistenceCamera.CreatedAt,
            Status = persistenceCamera.Metadata?.Status != null ? (CameraStatus)persistenceCamera.Metadata.Status : CameraStatus.Offline,
            LastConnectedAt = persistenceCamera.Metadata?.LastConnectedAt ?? DateTime.MinValue,
            Capabilities = DeserializeCapabilities(persistenceCamera.Metadata?.CapabilitiesJson),
            DeviceInfo = DeserializeDeviceInfo(persistenceCamera.Metadata?.DeviceInfoJson),
            Profiles = persistenceCamera.Profiles?.Select(p => p.ToSharedProfile()).ToList() ?? new()
        };
    }

    public static PersistenceCamera ToPersistenceCamera(this SharedCamera sharedCamera)
    {
        var persistenceCamera = new PersistenceCamera
        {
            Id = sharedCamera.Id,
            Name = sharedCamera.Name,
            Url = sharedCamera.Url.ToString(),
            Username = sharedCamera.Username,
            Password = sharedCamera.Password,
            Protocol = (int)sharedCamera.Protocol,
            CreatedAt = sharedCamera.CreatedAt
        };

        // Create metadata if needed
        persistenceCamera.Metadata = new PersistenceCameraMetadata
        {
            CameraId = sharedCamera.Id,
            Status = (int)sharedCamera.Status,
            LastConnectedAt = sharedCamera.LastConnectedAt,
            CapabilitiesJson = SerializeCapabilities(sharedCamera.Capabilities),
            DeviceInfoJson = SerializeDeviceInfo(sharedCamera.DeviceInfo)
        };

        // Create profiles
        persistenceCamera.Profiles = sharedCamera.Profiles?.Select(p => p.ToPersistenceProfile(sharedCamera.Id)).ToList() ?? new List<PersistenceCameraProfile>();

        return persistenceCamera;
    }

    public static void UpdatePersistenceCamera(this PersistenceCamera persistenceCamera, SharedCamera sharedCamera)
    {
        // Update basic camera properties
        persistenceCamera.Name = sharedCamera.Name;
        persistenceCamera.Url = sharedCamera.Url.ToString();
        persistenceCamera.Username = sharedCamera.Username;
        persistenceCamera.Password = sharedCamera.Password;
        persistenceCamera.Protocol = (int)sharedCamera.Protocol;

        // Update or create metadata
        if (persistenceCamera.Metadata == null)
        {
            persistenceCamera.Metadata = new PersistenceCameraMetadata
            {
                CameraId = persistenceCamera.Id
            };
        }

        persistenceCamera.Metadata.Status = (int)sharedCamera.Status;
        persistenceCamera.Metadata.LastConnectedAt = sharedCamera.LastConnectedAt;
        persistenceCamera.Metadata.CapabilitiesJson = SerializeCapabilities(sharedCamera.Capabilities);
        persistenceCamera.Metadata.DeviceInfoJson = SerializeDeviceInfo(sharedCamera.DeviceInfo);

        // Update profiles - clear and recreate for simplicity
        persistenceCamera.Profiles.Clear();
        if (sharedCamera.Profiles != null)
        {
            foreach (var sharedProfile in sharedCamera.Profiles)
            {
                persistenceCamera.Profiles.Add(sharedProfile.ToPersistenceProfile(persistenceCamera.Id));
            }
        }
    }

    public static SharedCameraProfile ToSharedProfile(this PersistenceCameraProfile persistenceProfile)
    {
        return new SharedCameraProfile
        {
            Token = persistenceProfile.Token,
            Name = persistenceProfile.Name,
            IsMainStream = persistenceProfile.IsMainStream,
            RtspUri = !string.IsNullOrEmpty(persistenceProfile.RtspUri) ? new Uri(persistenceProfile.RtspUri) : null,
            WebRtcUri = !string.IsNullOrEmpty(persistenceProfile.WebRtcUri) ? new Uri(persistenceProfile.WebRtcUri) : null,
            Video = DeserializeVideoConfig(persistenceProfile.VideoConfigJson),
            Audio = DeserializeAudioConfig(persistenceProfile.AudioConfigJson)
        };
    }

    public static PersistenceCameraProfile ToPersistenceProfile(this SharedCameraProfile sharedProfile, Guid cameraId)
    {
        return new PersistenceCameraProfile
        {
            Id = Guid.NewGuid(),
            CameraId = cameraId,
            Token = sharedProfile.Token,
            Name = sharedProfile.Name,
            IsMainStream = sharedProfile.IsMainStream,
            RtspUri = sharedProfile.RtspUri?.ToString(),
            WebRtcUri = sharedProfile.WebRtcUri?.ToString(),
            VideoConfigJson = SerializeVideoConfig(sharedProfile.Video),
            AudioConfigJson = SerializeAudioConfig(sharedProfile.Audio)
        };
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

    private static string SerializeVideoConfig(VideoSettings? videoConfig)
    {
        return videoConfig != null ? JsonSerializer.Serialize(videoConfig) : "{}";
    }

    private static VideoSettings DeserializeVideoConfig(string? videoConfigJson)
    {
        return !string.IsNullOrEmpty(videoConfigJson) && videoConfigJson != "{}" 
            ? JsonSerializer.Deserialize<VideoSettings>(videoConfigJson) ?? new()
            : new();
    }

    private static string SerializeAudioConfig(AudioSettings? audioConfig)
    {
        return audioConfig != null ? JsonSerializer.Serialize(audioConfig) : "{}";
    }

    private static AudioSettings? DeserializeAudioConfig(string? audioConfigJson)
    {
        return !string.IsNullOrEmpty(audioConfigJson) && audioConfigJson != "{}" 
            ? JsonSerializer.Deserialize<AudioSettings>(audioConfigJson) 
            : null;
    }
}