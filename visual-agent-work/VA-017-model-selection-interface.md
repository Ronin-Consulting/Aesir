# VA-017: Add Model Selection Interface

**Epic**: VISUAL_AGENT_UX
**Phase**: 5 - Configuration & Management
**Priority**: Medium
**Estimate**: 4 hours

## Description
Create UI for selecting and configuring DeepStream models (detector, classifier, tracker) with parameter tuning options.

## Acceptance Criteria
- [ ] Model type selector (Detector/Classifier/Tracker)
- [ ] Model dropdown with available models (YOLOv8, ResNet, etc.)
- [ ] Configuration parameters (confidence threshold, NMS threshold, batch size)
- [ ] Model info display (description, supported classes, performance)
- [ ] Save model configuration per agent
- [ ] Validation of parameter ranges

## UI Components
```xml
<StackPanel>
    <TextBlock>Detector Model</TextBlock>
    <ComboBox ItemsSource="{Binding AvailableDetectors}"
              SelectedItem="{Binding SelectedDetector}"/>

    <TextBlock>Confidence Threshold</TextBlock>
    <Slider Minimum="0" Maximum="1" Value="{Binding ConfidenceThreshold}"/>

    <TextBlock>NMS Threshold</TextBlock>
    <Slider Minimum="0" Maximum="1" Value="{Binding NmsThreshold}"/>

    <!-- Additional parameters... -->
</StackPanel>
```

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/ModelSelectionControl.axaml`
- `Aesir.Client/Aesir.Client/ViewModels/ModelSelectionViewModel.cs`
- `Aesir.Client/Aesir.Client/Models/ModelInfo.cs`

## Dependencies
- VA-011 (VisualAgentViewModel)

## Testing
- [ ] Model list populates from API
- [ ] Parameter changes update configuration
- [ ] Validation prevents invalid values
- [ ] Configuration saves to agent