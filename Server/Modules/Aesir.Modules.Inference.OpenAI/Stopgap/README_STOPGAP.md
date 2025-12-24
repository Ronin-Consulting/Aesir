# STOPGAP: Reasoning Content Interceptor

> **This is a temporary workaround.** Remove this entire folder when the OpenAI .NET SDK or Semantic Kernel adds native support for `reasoning_content` in streaming responses.

## Problem

llama.cpp's llama-server (used by TTRA and similar OpenAI-compatible inference servers) returns `delta.reasoning_content` in SSE streaming responses for reasoning models like GPT-OSS and DeepSeek. However:

1. The OpenAI .NET SDK's `StreamingChatCompletionUpdate` class does not have a `reasoning_content` property
2. The field is silently dropped during JSON deserialization
3. Semantic Kernel uses the SDK, so reasoning content never reaches our ChatService

## Solution

This stopgap injects a custom `DelegatingHandler` into the OpenAI client's HTTP transport that:

1. Intercepts SSE streaming responses
2. Parses each chunk for `reasoning_content`
3. Passes content to ChatService via Channel + AsyncLocal bridge
4. Leaves the original stream untouched for SDK consumption

## Files in This Folder

| File | Purpose |
|------|---------|
| `IReasoningContentCollector_Stopgap.cs` | Interface for the collector |
| `ReasoningContentCollector_Stopgap.cs` | Channel + AsyncLocal bridge for passing data |
| `ReasoningContentSseParser_Stopgap.cs` | JSON parsing for SSE chunks |
| `ReasoningContentHandler_Stopgap.cs` | DelegatingHandler + Stream wrapper |
| `README_STOPGAP.md` | This file |

## Modified Files

When removing this stopgap, you'll also need to revert changes in:

1. **`OpenAIInferenceModule.cs`**
   - Remove `using Aesir.Modules.Inference.OpenAI.Stopgap;`
   - Remove `using System.ClientModel.Primitives;`
   - Revert OpenAI client creation to simple version (no custom transport)

2. **`ChatService.cs`**
   - Remove `using Aesir.Modules.Inference.OpenAI.Stopgap;`
   - Remove `ReasoningContentCollector_Stopgap` creation and scope
   - Remove `MergeReasoningAndContent_Stopgap` method
   - Revert to simple streaming loop

## Removal Checklist

When OpenAI SDK or SK adds native `reasoning_content` support:

- [ ] Verify native support works with TTRA/llama-server
- [ ] Delete this entire `Stopgap/` folder
- [ ] Revert `OpenAIInferenceModule.cs` changes
- [ ] Revert `ChatService.cs` changes
- [ ] Search codebase for any remaining `_Stopgap` references
- [ ] Run all tests
- [ ] Update CLAUDE.md if any mentions exist

## Related Resources

- [llama.cpp server README](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md) - Documents `--reasoning-format` options
- [Open WebUI issue #17428](https://github.com/open-webui/open-webui/issues/17428) - Similar issue with Harmony format parsing
- [llama.cpp discussion #15341](https://github.com/ggml-org/llama.cpp/discussions/15341) - GPT-OSS and grammar with reasoning

## Known Issues

### Streaming Delay (TODO: Investigate)

**Issue**: The reasoning content collector is not properly streaming content to the client in real-time. The reasoning content only appears after the full response has completed, rather than streaming incrementally as chunks arrive.

**Expected behavior**: Reasoning content should stream to the client as each SSE chunk is parsed, providing real-time visibility into the model's reasoning process.

**Current behavior**: All reasoning content appears at once after the response completes.

**Investigation areas**:
- Check if Channel is being consumed properly in ChatService
- Verify the merge logic in `MergeReasoningAndContent_Stopgap` isn't buffering
- Ensure `IAsyncEnumerable` is yielding immediately, not batching
- Check if there's any buffering in the HTTP response pipeline
- Verify client-side SSE handling isn't waiting for completion

---

## Date Added

2025-12-24
