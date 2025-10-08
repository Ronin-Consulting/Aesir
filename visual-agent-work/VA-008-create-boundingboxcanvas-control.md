# VA-008: Create BoundingBoxCanvas Control

**Epic**: VISUAL_AGENT_UX
**Phase**: 3 - Detection Overlay
**Priority**: High
**Estimate**: 6 hours

## Description
Create a custom Avalonia control that renders bounding boxes, labels, and tracking data over video streams using the Avalonia `DrawingContext` API.

## Acceptance Criteria
- [ ] `BoundingBoxCanvas` inherits from `Control`
- [ ] Override `Render(DrawingContext)` to draw detections
- [ ] Observable property for `Detections` collection
- [ ] Draw rectangles with configurable colors/thickness
- [ ] Render class labels and confidence scores
- [ ] Handle overlay transparency
- [ ] Performance: 30+ FPS rendering

## Technical Details
```csharp
public class BoundingBoxCanvas : Control
{
    public static readonly StyledProperty<ObservableCollection<Detection>> DetectionsProperty = ...;

    public override void Render(DrawingContext context)
    {
        foreach (var detection in Detections)
        {
            var rect = new Rect(
                detection.BoundingBox.X,
                detection.BoundingBox.Y,
                detection.BoundingBox.Width,
                detection.BoundingBox.Height
            );

            var pen = new Pen(GetClassColor(detection.ClassId), 2);
            context.DrawRectangle(pen, rect);

            // Draw label
            var formattedText = new FormattedText(...);
            context.DrawText(formattedText, new Point(rect.X, rect.Y - 20));
        }
    }
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/BoundingBoxCanvas.cs`

## Dependencies
- VA-002 (NativeVideoPlayerControl)

## Testing
- [ ] Bounding boxes render correctly
- [ ] Labels display above boxes
- [ ] Colors differentiate classes
- [ ] Performance acceptable with 50+ detections
- [ ] Overlay updates in real-time