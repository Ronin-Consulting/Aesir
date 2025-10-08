# DeepStream Visual Agent Integration

## Overview

This document outlines the integration of NVIDIA DeepStream SDK with the AESIR platform to provide visual AI agent capabilities for video analytics. The implementation follows a phased approach that allows development without physical Jetson hardware by using mock services and synthetic data.

## What is DeepStream?

NVIDIA DeepStream is an SDK for developing AI-powered video analytics applications on NVIDIA GPUs and Jetson platforms. It provides:

- **High-performance inference engine** with preprocessing, post-processing, and stream management
- **Modular architecture** with components for network preprocessing, postprocessing, and stream management
- **Rich metadata system** for tracking batch info, frame metadata, object detection, classification, and segmentation results
- **GStreamer integration** with plugins like `nvdsosd`, `nvdsinfer`, `nvdsanalytics`, `nvdspreprocess`
- **Multiple network types** supported including classifiers, detectors, segmentation models
- **Multi-GPU support** with per-device resource allocation

## Architecture

### High-Level Flow

```
Video Source (RTSP/File)
    → DeepStream Pipeline (Jetson/GPU)
        → Inference (Object Detection/Tracking/Classification)
            → Analytics Events
                → AESIR API Server (via HTTP/gRPC/WebSocket)
                    → PostgreSQL (Event Storage)
                    → Client UI (Real-time Display)
```

### Component Integration

1. **DeepStream Service** - Standalone service/container running DeepStream pipelines
2. **AESIR API Server** - Receives analytics results, stores events, manages configuration
3. **Visual Agent Service** - C# service layer implementing `IVisualAgentService`
4. **Client UI** - Avalonia/Blazor components for configuration and visualization
5. **Message Queue** - Redis/RabbitMQ for async real-time event streaming

## Development Phases

### Phase 1: Core Service Architecture (No Hardware Required)

#### 1.1 Service Interfaces

Create the following interfaces in `Aesir.Api.Server/Services/`:

- `IVisualAgentService` - Main service for video analytics operations
- `IVideoSourceService` - Manage RTSP streams and video file sources
- `IAnalyticsEventService` - Handle detection/tracking events

#### 1.2 Data Models

Create models in `Aesir.Common/Models/`:

**VisualAgentRequest.cs**
```csharp
public class VisualAgentRequest
{
    public Guid? VideoSourceId { get; set; }
    public string? ModelType { get; set; } // "detector", "classifier", "tracker"
    public Dictionary<string, string>? ModelConfig { get; set; }
    public List<RegionOfInterest>? ROIs { get; set; }
    public Dictionary<string, object>? AlertRules { get; set; }
}
```

**VisualAgentResult.cs**
```csharp
public class VisualAgentResult
{
    public Guid? ResultId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<Detection>? Detections { get; set; }
    public List<Track>? Tracks { get; set; }
    public List<AnalyticsEvent>? Events { get; set; }
}
```

**Detection.cs**
```csharp
public class Detection
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public float Confidence { get; set; }
    public BoundingBox? BoundingBox { get; set; }
}
```

**Track.cs**
```csharp
public class Track
{
    public long TrackId { get; set; }
    public int ClassId { get; set; }
    public List<BoundingBox>? Trajectory { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}
```

**BoundingBox.cs**
```csharp
public class BoundingBox
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}
```

**RegionOfInterest.cs**
```csharp
public class RegionOfInterest
{
    public string? Name { get; set; }
    public List<Point>? Polygon { get; set; }
    public string? AlertType { get; set; } // "intrusion", "loitering", "counting"
}
```

#### 1.3 Agent Extension

Extend `AesirAgentBase` in `Aesir.Common/Models/`:

```csharp
public class AesirAgentBase
{
    // ... existing properties ...

    // Visual Agent Properties
    [JsonPropertyName("video_source_id")]
    public Guid? VideoSourceId { get; set; }

    [JsonPropertyName("detector_model")]
    public string? DetectorModel { get; set; }

    [JsonPropertyName("tracker_config")]
    public Dictionary<string, object>? TrackerConfig { get; set; }

    [JsonPropertyName("regions_of_interest")]
    public List<RegionOfInterest>? RegionsOfInterest { get; set; }

    [JsonPropertyName("alert_rules")]
    public Dictionary<string, object>? AlertRules { get; set; }
}
```

#### 1.4 Inference Engine Type

Add to `Aesir.Common/Models/InferenceEngineType.cs` (or create if doesn't exist):

```csharp
public enum InferenceEngineType
{
    Ollama,
    OpenAICompatible,
    DeepStream  // New type
}
```

### Phase 2: Database Schema

Create migration in `Aesir.Api.Server/Data/Migrations/`:

**Tables to Add:**

```sql
-- Video sources (RTSP streams, video files)
CREATE TABLE video_sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    source_type VARCHAR(50) NOT NULL, -- 'rtsp', 'file', 'webcam'
    source_uri TEXT NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Visual agents (extends agent concept)
CREATE TABLE visual_agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    agent_id UUID REFERENCES agents(id),
    video_source_id UUID REFERENCES video_sources(id),
    detector_model VARCHAR(255),
    tracker_config JSONB,
    regions_of_interest JSONB,
    alert_rules JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Analytics events (detections, tracks, alerts)
CREATE TABLE analytics_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    visual_agent_id UUID REFERENCES visual_agents(id),
    event_type VARCHAR(50) NOT NULL, -- 'detection', 'track', 'alert'
    event_data JSONB NOT NULL,
    frame_timestamp TIMESTAMP NOT NULL,
    confidence FLOAT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Video metadata (processing stats)
CREATE TABLE video_metadata (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    video_source_id UUID REFERENCES video_sources(id),
    frame_count BIGINT,
    fps FLOAT,
    processing_latency_ms FLOAT,
    timestamp TIMESTAMP DEFAULT NOW()
);
```

### Phase 3: Mock Implementation

Create `Aesir.Api.Server/Services/Implementations/DeepStream/MockVisualAgentService.cs`:

```csharp
public class MockVisualAgentService : IVisualAgentService
{
    private readonly ILogger<MockVisualAgentService> _logger;
    private readonly Random _random = new();

    public MockVisualAgentService(ILogger<MockVisualAgentService> logger)
    {
        _logger = logger;
    }

    public async Task<VisualAgentResult> AnalyzeVideoAsync(VisualAgentRequest request, CancellationToken cancellationToken = default)
    {
        // Simulate processing delay
        await Task.Delay(100, cancellationToken);

        // Generate synthetic detections
        var detections = GenerateMockDetections(5);
        var tracks = GenerateMockTracks(3);
        var events = GenerateMockEvents(2);

        return new VisualAgentResult
        {
            ResultId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Detections = detections,
            Tracks = tracks,
            Events = events
        };
    }

    public IAsyncEnumerable<VisualAgentResult> StreamAnalyticsAsync(VisualAgentRequest request, CancellationToken cancellationToken = default)
    {
        return StreamAnalyticsInternalAsync(request, cancellationToken);
    }

    private async IAsyncEnumerable<VisualAgentResult> StreamAnalyticsInternalAsync(
        VisualAgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simulate 30 FPS stream
        var frameInterval = TimeSpan.FromMilliseconds(33);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(frameInterval, cancellationToken);

            yield return new VisualAgentResult
            {
                ResultId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Detections = GenerateMockDetections(_random.Next(1, 8)),
                Tracks = GenerateMockTracks(_random.Next(1, 5)),
                Events = GenerateMockEvents(_random.Next(0, 3))
            };
        }
    }

    private List<Detection> GenerateMockDetections(int count)
    {
        var classes = new[] { "person", "car", "bicycle", "dog", "cat" };
        var detections = new List<Detection>();

        for (int i = 0; i < count; i++)
        {
            detections.Add(new Detection
            {
                ClassId = _random.Next(0, classes.Length),
                ClassName = classes[_random.Next(0, classes.Length)],
                Confidence = (float)(_random.NextDouble() * 0.4 + 0.6), // 0.6-1.0
                BoundingBox = new BoundingBox
                {
                    X = (float)(_random.NextDouble() * 800),
                    Y = (float)(_random.NextDouble() * 600),
                    Width = (float)(_random.NextDouble() * 200 + 50),
                    Height = (float)(_random.NextDouble() * 200 + 50)
                }
            });
        }

        return detections;
    }

    private List<Track> GenerateMockTracks(int count)
    {
        var tracks = new List<Track>();

        for (int i = 0; i < count; i++)
        {
            var trajectory = new List<BoundingBox>();
            var baseX = (float)(_random.NextDouble() * 600);
            var baseY = (float)(_random.NextDouble() * 400);

            for (int j = 0; j < 5; j++)
            {
                trajectory.Add(new BoundingBox
                {
                    X = baseX + j * 10,
                    Y = baseY + j * 5,
                    Width = 100,
                    Height = 150
                });
            }

            tracks.Add(new Track
            {
                TrackId = _random.Next(1000, 9999),
                ClassId = _random.Next(0, 5),
                Trajectory = trajectory,
                FirstSeen = DateTime.UtcNow.AddSeconds(-5),
                LastSeen = DateTime.UtcNow
            });
        }

        return tracks;
    }

    private List<AnalyticsEvent> GenerateMockEvents(int count)
    {
        var eventTypes = new[] { "zone_intrusion", "loitering", "crowd_detected", "object_left" };
        var events = new List<AnalyticsEvent>();

        for (int i = 0; i < count; i++)
        {
            events.Add(new AnalyticsEvent
            {
                EventType = eventTypes[_random.Next(0, eventTypes.Length)],
                Confidence = (float)(_random.NextDouble() * 0.3 + 0.7),
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["zone_name"] = $"Zone_{_random.Next(1, 4)}",
                    ["object_count"] = _random.Next(1, 10)
                }
            });
        }

        return events;
    }
}
```

### Phase 4: API Controller

Create `Aesir.Api.Server/Controllers/VisualAgentController.cs`:

```csharp
[ApiController]
[Route("visualagent")]
public class VisualAgentController : ControllerBase
{
    private readonly IVisualAgentService _visualAgentService;
    private readonly ILogger<VisualAgentController> _logger;

    public VisualAgentController(
        IVisualAgentService visualAgentService,
        ILogger<VisualAgentController> logger)
    {
        _visualAgentService = visualAgentService;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<VisualAgentResult>> AnalyzeVideo(
        [FromBody] VisualAgentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _visualAgentService.AnalyzeVideoAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stream")]
    public async IAsyncEnumerable<VisualAgentResult> StreamAnalytics(
        [FromQuery] Guid videoSourceId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new VisualAgentRequest { VideoSourceId = videoSourceId };

        await foreach (var result in _visualAgentService.StreamAnalyticsAsync(request, cancellationToken))
        {
            yield return result;
        }
    }
}
```

### Phase 5: Configuration

Update `Aesir.Api.Server/appsettings.json`:

```json
{
  "DeepStream": {
    "Enabled": true,
    "MockMode": true,
    "GrpcEndpoint": "http://localhost:50051",
    "DefaultModels": {
      "Detector": "yolov8n",
      "Classifier": "resnet50",
      "Tracker": "nvdcf"
    },
    "InferenceConfig": {
      "BatchSize": 1,
      "FrameSkip": 0,
      "ConfidenceThreshold": 0.5,
      "NmsThreshold": 0.4
    },
    "OutputConfig": {
      "EnableRtspOutput": true,
      "RtspPort": 8554,
      "EnableFileOutput": false
    }
  }
}
```

Register in `Program.cs`:

```csharp
// In the inference engine loop, add DeepStream case
case InferenceEngineType.DeepStream:
{
    builder.Services.AddKeyedTransient<IVisualAgentService>(
        inferenceEngineIdKey,
        (serviceProvider, key) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<MockVisualAgentService>>();

        // For now, always use mock. Later: check config to decide mock vs real
        return new MockVisualAgentService(logger);
    });
    break;
}
```

### Phase 6: Client UI Components

Create Avalonia components in `Aesir.Client/Aesir.Client/`:

**ViewModels/VisualAgentViewModel.cs** - Main ViewModel for visual agent configuration and display

**Views/VisualAgentView.axaml** - XAML view with video player, bounding box canvas, analytics panel

**Controls/VideoPlayerControl.axaml** - Reusable video player with overlay support

**Controls/BoundingBoxCanvas.axaml** - Custom canvas for drawing detections/tracks

## How to Interact with Mock Visual Agent

### 1. API Endpoint Testing

Use the following endpoints to test the mock service:

#### Analyze Single Frame/Video Segment

```bash
curl -X POST https://aesir.localhost/visualagent/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "videoSourceId": "00000000-0000-0000-0000-000000000001",
    "modelType": "detector",
    "modelConfig": {
      "model_name": "yolov8n",
      "confidence_threshold": "0.5"
    }
  }'
```

**Response:**
```json
{
  "resultId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-10-07T12:34:56Z",
  "detections": [
    {
      "classId": 0,
      "className": "person",
      "confidence": 0.87,
      "boundingBox": {
        "x": 245.5,
        "y": 123.8,
        "width": 85.3,
        "height": 210.6
      }
    }
  ],
  "tracks": [...],
  "events": [...]
}
```

#### Stream Real-time Analytics

```bash
# Using curl (will stream JSON objects)
curl https://aesir.localhost/visualagent/stream?videoSourceId=00000000-0000-0000-0000-000000000001
```

Or use SignalR WebSocket client from Avalonia UI.

### 2. Testing with Sample Videos

Place test videos in `Aesir.Api.Server/Assets/TestVideos/`:

- `traffic.mp4` - Traffic monitoring scenario
- `retail.mp4` - Retail store monitoring
- `warehouse.mp4` - Industrial setting

Mock service will simulate processing these videos.

### 3. Synthetic RTSP Stream (Using FFmpeg)

Generate a test RTSP stream locally:

```bash
# Install FFmpeg first
# macOS: brew install ffmpeg
# Linux: apt install ffmpeg

# Stream a test pattern
ffmpeg -re -f lavfi -i testsrc=size=1280x720:rate=30 \
  -vcodec libx264 -f rtsp rtsp://localhost:8554/test

# Or stream a video file in loop
ffmpeg -re -stream_loop -1 -i traffic.mp4 \
  -vcodec libx264 -f rtsp rtsp://localhost:8554/traffic
```

Configure in AESIR:
```json
{
  "videoSourceId": "test-rtsp-1",
  "sourceType": "rtsp",
  "sourceUri": "rtsp://localhost:8554/test"
}
```

### 4. Client UI Testing

1. **Launch Desktop Client**: `dotnet run --project Aesir.Client/Aesir.Client.Desktop`
2. **Navigate to Visual Agents** section
3. **Create New Visual Agent**:
   - Select video source (mock RTSP or file)
   - Choose detector model (YOLOv8, YOLOv5, etc.)
   - Draw ROIs on canvas
   - Configure alert rules
4. **Start Analytics** - View real-time detections overlaid on video
5. **View Analytics Dashboard** - See detection counts, event timeline, track histories

### 5. Development Workflow

```bash
# 1. Start API server with mock DeepStream
docker compose -f docker-compose-api-dev.yml up

# 2. In another terminal, start FFmpeg RTSP stream
ffmpeg -re -f lavfi -i testsrc=size=1280x720:rate=30 \
  -vcodec libx264 -f rtsp rtsp://localhost:8554/test

# 3. Start desktop client
cd Aesir.Client/Aesir.Client.Desktop
dotnet run

# 4. In client UI:
#    - Create visual agent
#    - Point to rtsp://localhost:8554/test
#    - Start analytics
#    - See mock detections overlaid on test pattern
```

## Future: Real DeepStream Integration

When Jetson hardware is available:

1. **Deploy DeepStream Pipeline**:
   - Create GStreamer pipeline configuration
   - Configure TensorRT model conversion
   - Set up RTSP output stream

2. **Create Production Service**:
   - Replace `MockVisualAgentService` with `DeepStreamVisualAgentService`
   - Implement gRPC client to DeepStream server
   - Parse DeepStream metadata (NvDsBatchMeta, NvDsObjectMeta)

3. **Hardware Configuration**:
   - Update docker-compose with GPU runtime
   - Configure NVIDIA Container Toolkit
   - Set up video source connections

4. **Performance Tuning**:
   - Optimize batch size and frame skip
   - Enable TensorRT FP16/INT8 quantization
   - Configure multi-stream processing

## References

- [NVIDIA DeepStream Documentation](https://docs.nvidia.com/metropolis/deepstream/dev-guide/)
- [DeepStream Python Apps](https://github.com/NVIDIA-AI-IOT/deepstream_python_apps)
- [GStreamer Documentation](https://gstreamer.freedesktop.org/documentation/)
- [AESIR Architecture](./README.md)

## Next Steps

1. Review and approve this design document
2. Create database migrations for visual agent tables
3. Implement mock service and API endpoints
4. Build client UI components for video analytics
5. Test end-to-end with synthetic data
6. Prepare for hardware integration when available