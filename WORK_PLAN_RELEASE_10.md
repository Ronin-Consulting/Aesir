# WORK_PLAN_RELEASE_10.md

> **STATUS: COMPLETE**
>
> File Attachment UI/UX Refinement
>
> **Scope:** Client/Aesir.Client.Web/ (Blazor WebAssembly)
> **Priority:** Medium - Improves user experience and visual consistency
>
> **Completed:** All epics finished. Responsive Design (5.2) and Accessibility (5.3) deferred to future release.

This work plan documents UI/UX refinements for file attachments across the Blazor WebAssembly client, aligning the design with modern AI assistant interfaces like Claude and ChatGPT.

## Overview

### Current State

The current file attachment implementation includes:
- `FileAttachment.razor` - Unified component with Pending/Attached modes
- `FileChip.razor` - Legacy MudChip-based component
- Files displayed in user messages and message input area
- Citations rendered as styled markdown links in AI responses

### Issues to Address

1. **Pending Files (Message Input)**
   - Current design uses pill-shaped chips that feel outdated
   - No image thumbnail preview for visual files
   - Progress indicator could be more prominent
   - Remove button placement is inconsistent

2. **Files in User Messages**
   - Files appear as separate elements within the message bubble
   - Styling doesn't integrate well with the bubble design
   - No visual distinction between file types
   - Deleted file styling could be clearer

3. **Citations in AI Responses**
   - Citation links blend into regular text
   - No visual preview or file type indicator inline
   - Requires hovering to identify as a file reference

4. **General**
   - Inconsistent styling between pending and attached states
   - `FileChip.razor` duplicates functionality
   - Icon selection could be more distinctive

### Goal

Create a cohesive, modern file attachment experience that:
1. Looks clean and professional like Claude/ChatGPT
2. Provides clear visual feedback for file states (uploading, uploaded, error, deleted)
3. Shows image thumbnails for visual file types
4. Makes citations clearly identifiable in AI responses
5. Maintains consistency across all file display contexts

## Legend

- [ ] Not started
- [x] Completed
- [~] Skipped (with reason in comments)

---

## Epic 1: Design System & Shared Components

> **PRIORITY: HIGH** - Foundation for consistent styling

### 1.1 Create File Type Icon System

**Goal:** Consistent, distinctive icons for different file types.

**Work Items:**
- [x] 1.1.1 Define file type categories (document, image, data, code, archive, other)
- [x] 1.1.2 Select/create distinctive icons for each category
- [x] 1.1.3 Create `FileTypeHelper` service for icon and color mapping
- [x] 1.1.4 Add file type accent colors (PDF=red, Image=blue, Code=green, etc.)

---

### 1.2 Create Base File Card Component

**Goal:** Modern card-based file display component.

**New File:** `Aesir.Client.Web.Modules.Chat/Components/FileCard.razor`

**Design Characteristics (Claude/ChatGPT-inspired):**
- Rounded rectangle card (not pill-shaped)
- Left: File type icon with accent color background
- Center: Filename (truncated) and file size
- Right: Action buttons (remove, download, view)
- Subtle border and shadow
- Hover state with slight elevation

**Work Items:**
- [x] 1.2.1 Create base `FileCard` component structure
- [x] 1.2.2 Implement file type icon with colored background
- [x] 1.2.3 Add filename display with smart truncation (middle ellipsis)
- [x] 1.2.4 Add file size display
- [x] 1.2.5 Implement hover state with action buttons reveal
- [x] 1.2.6 Add click handler for file preview/download
- [x] 1.2.7 Style for dark/light mode compatibility

---

### 1.3 Create Image Thumbnail Component

**Goal:** Show image previews for visual file types.

**New File:** `Aesir.Client.Web.Modules.Chat/Components/ImageThumbnail.razor`

**Features:**
- Lazy-loaded thumbnail for images
- Fallback to file icon on load failure
- Click to open full preview
- Loading skeleton while image loads

**Work Items:**
- [x] 1.3.1 Create `ImageThumbnail` component
- [~] 1.3.2 Implement lazy loading with intersection observer (deferred - using basic loading states)
- [x] 1.3.3 Add loading skeleton placeholder
- [x] 1.3.4 Add error fallback to file icon
- [x] 1.3.5 Integrate with Citation Viewer on click

---

### 1.4 Consolidate/Deprecate Legacy Components

**Goal:** Clean up redundant file display components.

**Work Items:**
- [~] 1.4.1 Audit usage of `FileChip.razor` (deferred - keeping for compatibility)
- [~] 1.4.2 Migrate usages to new `FileCard` component (partially done - key areas updated)
- [~] 1.4.3 Mark `FileChip.razor` as deprecated or remove (deferred)
- [~] 1.4.4 Update `FileAttachment.razor` to use new shared styles (deferred - using FileCard in new areas)

---

## Epic 2: Pending Files in Message Input

> **PRIORITY: HIGH** - Most frequently seen file display

### 2.1 Redesign Pending Files Area

**Goal:** Modern, compact file cards above the input field.

**File:** `Aesir.Client.Web.Modules.Chat/Components/MessageInput.razor`

**Design:**
- Horizontal scrolling row for multiple files (not wrapping)
- Each file as a compact card (Claude-style)
- Clear visual state for uploading/uploaded/error
- Smooth add/remove animations

**Work Items:**
- [x] 2.1.1 Replace current pending files section with horizontal scroll container
- [x] 2.1.2 Use new `FileCard` component in compact mode
- [x] 2.1.3 Add upload progress bar integrated into card
- [~] 2.1.4 Implement smooth add/remove animations (CSS transitions) (deferred to Epic 5)
- [~] 2.1.5 Add drag handle for reordering (optional) (deferred)
- [x] 2.1.6 Style scrollbar for horizontal overflow

---

### 2.2 Image Thumbnail in Pending Files

**Goal:** Show image previews before sending.

**Work Items:**
- [x] 2.2.1 Detect image file types from pending files
- [x] 2.2.2 Generate client-side thumbnail from File object (base64 data URL)
- [x] 2.2.3 Display thumbnail in place of icon for images
- [x] 2.2.4 Add loading state while thumbnail generates
- [x] 2.2.5 Handle thumbnail generation failure gracefully

---

### 2.3 Upload State Indicators

**Goal:** Clear visual feedback for upload progress.

**Work Items:**
- [x] 2.3.1 Design upload progress indicator (thin bar at bottom of card)
- [x] 2.3.2 Add "Uploading..." text state (via spinner overlay)
- [~] 2.3.3 Add "Processing..." state for indexing (deferred - uses same spinner)
- [~] 2.3.4 Add checkmark animation on complete (deferred to Epic 5)
- [x] 2.3.5 Add error state with retry option

---

## Epic 3: Files in User Messages

> **PRIORITY: HIGH** - Visible in conversation history

### 3.1 Redesign Attached Files in User Bubble

**Goal:** Clean integration of files with user message bubble.

**File:** `Aesir.Client.Web.Modules.Chat/Components/UserMessage.razor`

**Design Options:**
- **Option A:** Files above text (current) - refined styling
- **Option B:** Files as inline chips within text
- **Option C:** Files in a collapsible section

**Work Items:**
- [x] 3.1.1 Update attached files container styling
- [x] 3.1.2 Use `FileCard` component in "attached" mode
- [x] 3.1.3 Handle single file vs. multiple files layout
- [x] 3.1.4 Ensure contrast works in dark/light message bubbles
- [x] 3.1.5 Add subtle separator between files and message text

---

### 3.2 Image Display in User Messages

**Goal:** Show image attachments as visible thumbnails.

**Work Items:**
- [x] 3.2.1 Detect image attachments in user messages
- [x] 3.2.2 Display image thumbnails (larger than pending files)
- [x] 3.2.3 Support multiple images in a grid layout
- [x] 3.2.4 Click to open in Citation Viewer
- [x] 3.2.5 Handle mixed file types (images + documents)

---

### 3.3 Deleted File State

**Goal:** Clear indication when attached file no longer exists.

**Work Items:**
- [x] 3.3.1 Refine deleted file styling (grayed out, strikethrough)
- [x] 3.3.2 Add "File no longer available" tooltip (via placeholder)
- [x] 3.3.3 Remove click handler for deleted files
- [~] 3.3.4 Consider hiding delete state entirely (just icon change) (kept visible with placeholder)

---

## Epic 4: Citations in AI Responses

> **PRIORITY: MEDIUM** - Enhances readability of AI responses

### 4.1 Inline Citation Badge

**Goal:** Make citations visually distinct from regular links.

**File:** `Aesir.Client.Web.Modules.Chat/Services/MarkdownService.cs`

**Design (Claude-inspired):**
- Small pill/badge with file icon
- Filename visible (truncated)
- Page number badge if present
- Hover reveals full filename

**Work Items:**
- [x] 4.1.1 Update markdown renderer to detect citation links (already exists)
- [x] 4.1.2 Replace citation links with styled badge/chip (pill style with rounded corners)
- [x] 4.1.3 Add file type icon to citation badge
- [x] 4.1.4 Show page number badge for PDF citations
- [x] 4.1.5 Ensure citations don't break text flow (inline-flex with vertical-align)
- [x] 4.1.6 Add hover state with full filename tooltip

---

### 4.2 Citation Source List (Optional)

**Goal:** Show all cited sources at end of AI response.

**Design:**
- "Sources" section at bottom of response
- List of all unique files cited
- Collapsible if many sources

**Work Items:**
- [~] 4.2.1 Extract all citation links from response (deferred - optional feature)
- [~] 4.2.2 Deduplicate and group by file (deferred)
- [~] 4.2.3 Create "Sources" footer component (deferred)
- [~] 4.2.4 Add collapse/expand for 3+ sources (deferred)
- [~] 4.2.5 Click to open Citation Viewer (deferred)

---

### 4.3 Citation Hover Preview (Optional)

**Goal:** Quick preview on citation hover.

**Work Items:**
- [~] 4.3.1 Create hover card component for citations (deferred - optional feature)
- [~] 4.3.2 Show file type, name, size, and thumbnail (if image) (deferred)
- [~] 4.3.3 Add delay before showing (prevent flicker) (deferred)
- [~] 4.3.4 Include "Click to view" hint (deferred)

---

## Epic 5: Animation & Polish

> **PRIORITY: LOW** - Final refinements

### 5.1 Add Micro-animations

**Goal:** Subtle animations for professional feel.

**Work Items:**
- [x] 5.1.1 Add fade-in animation when files appear (already existed)
- [x] 5.1.2 Add scale animation on file card hover (enhanced with scale(1.01) and improved shadow)
- [x] 5.1.3 Add slide-out animation when file removed (fileSlideOut keyframes + IsRemoving state)
- [x] 5.1.4 Add progress bar animation for uploads (already existed with transition)
- [x] 5.1.5 Add checkmark animation on upload complete (successPop keyframes + ShowSuccess state)

---

### 5.2 Responsive Design [DEFERRED]

**Goal:** Files look good on all screen sizes.

**Status:** Deferred to future release.

**Work Items:**
- [ ] 5.2.1 Test file cards on mobile viewport
- [ ] 5.2.2 Adjust file card sizing for small screens
- [ ] 5.2.3 Ensure horizontal scroll works with touch
- [ ] 5.2.4 Test image thumbnails on various resolutions

---

### 5.3 Accessibility [DEFERRED]

**Goal:** Ensure file displays are accessible.

**Status:** Deferred to future release.

**Work Items:**
- [ ] 5.3.1 Add proper ARIA labels to file cards
- [ ] 5.3.2 Ensure keyboard navigation works
- [ ] 5.3.3 Add screen reader announcements for upload states
- [ ] 5.3.4 Verify color contrast for all states

---

## Epic 6: Testing

> **PRIORITY: HIGH** - Quality assurance

### 6.1 Unit Tests

**Work Items:**
- [x] 6.1.1 Test `FileTypeHelper` icon/color mapping (116 tests in FileTypeHelperTests.cs)
- [x] 6.1.2 Test filename truncation logic (included in FileTypeHelperTests.cs)
- [x] 6.1.3 Test file size formatting (included in FileTypeHelperTests.cs)

---

### 6.2 Component Tests

**Work Items:**
- [x] 6.2.1 Test `FileCard` component rendering (45 tests in FileCardTests.cs)
- [x] 6.2.2 Test `ImageThumbnail` loading states (14 tests in ImageThumbnailTests.cs)
- [x] 6.2.3 Test citation badge rendering (16 tests in CitationViewerTests.cs)

---

### 6.3 Manual Testing Checklist [COMPLETE]

**Pending Files:**
- [x] Single file upload
- [x] Multiple file upload
- [x] Upload progress display
- [x] Upload error handling
- [x] File removal
- [x] Image thumbnail preview

**User Messages:**
- [x] Single file attachment
- [x] Multiple file attachments
- [x] Image attachments
- [x] Mixed file types
- [x] Deleted file display
- [x] Dark/light mode

**Citations:**
- [x] PDF citation with page number
- [x] Image citation
- [x] Text file citation
- [x] Multiple citations in one response
- [x] Citation click opens viewer

---

## Architecture Decisions

### 1. Card vs. Chip Design

**Decision:** Use card-based design for files.

**Rationale:**
- Cards provide more space for information
- Better alignment with Claude/ChatGPT aesthetics
- Easier to add thumbnails and actions
- More scalable for future features

### 2. Image Thumbnails

**Decision:** Generate thumbnails client-side for pending, server-side for attached.

**Rationale:**
- Client-side for pending avoids server round-trip
- Server-side for attached ensures consistency
- Allows caching of generated thumbnails

### 3. Citation Styling

**Decision:** Use inline badge/chip for citations, not hover preview.

**Rationale:**
- Badges are visible without interaction
- Don't disrupt reading flow
- Match Claude's citation style
- Hover preview can be added later as enhancement

---

## Design Specifications

### File Card Dimensions

| Context | Width | Height | Icon Size |
|---------|-------|--------|-----------|
| Pending (compact) | 180px | 48px | 24px |
| Attached (message) | 220px | 56px | 32px |
| Image thumbnail | 120px | 80px | N/A |

### File Type Colors

| Type | Icon Color | Background |
|------|------------|------------|
| PDF | #E53935 | rgba(229, 57, 53, 0.1) |
| Image | #1E88E5 | rgba(30, 136, 229, 0.1) |
| Document | #43A047 | rgba(67, 160, 71, 0.1) |
| Data (JSON/CSV/XML) | #FB8C00 | rgba(251, 140, 0, 0.1) |
| Code | #8E24AA | rgba(142, 36, 170, 0.1) |
| Archive | #6D4C41 | rgba(109, 76, 65, 0.1) |
| Other | #757575 | rgba(117, 117, 117, 0.1) |

### Citation Badge Dimensions

| Element | Size |
|---------|------|
| Badge height | 22px |
| Icon size | 14px |
| Font size | 12px |
| Border radius | 4px |
| Max width | 200px |

---

## File Structure

```
Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/
├── Components/
│   ├── FileCard.razor                    # NEW - Modern file card component
│   ├── FileCard.razor.css               # NEW - File card styles
│   ├── ImageThumbnail.razor             # NEW - Image thumbnail component
│   ├── CitationBadge.razor              # NEW - Inline citation display
│   ├── CitationSourceList.razor         # NEW - Sources footer (optional)
│   ├── FileAttachment.razor             # MODIFY - Use shared styles
│   ├── FileChip.razor                   # DEPRECATE - Replace with FileCard
│   ├── UserMessage.razor                # MODIFY - Update file display
│   ├── MessageInput.razor               # MODIFY - Update pending files
│   └── AssistantMessage.razor           # MODIFY - If adding sources footer
│
├── Services/
│   ├── FileTypeHelper.cs                # NEW - File type icon/color mapping
│   └── MarkdownService.cs               # MODIFY - Citation badge rendering
```

---

## Success Criteria

- [ ] File cards look modern and consistent across all contexts
- [ ] Image thumbnails display for visual file types
- [ ] Upload progress is clearly visible
- [ ] Citations are visually distinct from regular links
- [ ] Dark/light mode works correctly
- [ ] No regression in existing file functionality
- [ ] Responsive design works on mobile
- [ ] Accessibility requirements met

---

## References

- Claude.ai file attachment design
- ChatGPT file attachment design
- Material Design 3 chip/card components
- Existing `FileAttachment.razor` implementation
- Release 9 Citation Viewer implementation

---

## Future Enhancements (Out of Scope)

1. **Drag and drop reordering** - Reorder pending files before sending
2. **File preview in hover** - Quick preview without opening viewer
3. **Batch file operations** - Select multiple files for removal
4. **File compression** - Compress large files before upload
5. **Cloud storage integration** - Attach files from Google Drive, Dropbox
