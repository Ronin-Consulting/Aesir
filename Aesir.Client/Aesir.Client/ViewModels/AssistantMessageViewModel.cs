using System;
using System.Collections.Generic;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger, 
    IMarkdownService markdownService,
    IPdfViewerService pdfViewerService) : MessageViewModel(logger, markdownService)
{
    public override string Role => "assistant";
    
    protected override string NormalizeInput(string input)
    {
        // for thinking models the input will have a <think> tags.  
        // for now just log the think but remove it until we have place for it in UI
        
        // Extract anything between <think> tags (including the tags themselves)
        var startIndex = input.IndexOf("<think>", StringComparison.InvariantCultureIgnoreCase);

        if (startIndex < 0) return input;
        
        var endIndex = input.IndexOf("</think>", startIndex, StringComparison.InvariantCultureIgnoreCase);
        
        if (endIndex >= 0)
        {
            // Remove everything from start of <think> to end of </think> (including tags)
            input = input.Remove(startIndex, (endIndex + "</think>".Length) - startIndex);
        }
        else
        {
            // If closing tag is missing, just remove the opening tag as before
            input = input.Replace("<think>", "");
        }

        return input;
    }

    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        pdfViewerService.ShowPdfAsync(link);
    }
}