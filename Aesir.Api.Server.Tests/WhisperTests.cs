//using Whisper.net.Ggml;

namespace Aesir.Api.Server.Tests;

[TestClass]
public class WhisperTests
{
    [TestMethod]
    public async Task SimpleIntegrationTest()
    {
        // // We declare three variables which we will use later, ggmlType, modelFileName and wavFileName
        // const GgmlType ggmlType = GgmlType.Base;
        // const string modelFileName = "ggml-base.bin";
        // const string wavFileName = "kennedy.wav";
        //
        // LogProvider.AddConsoleLogging(WhisperLogLevel.Cont);
        //
        // // Optional set the order of the runtimes:
        // RuntimeOptions.Instance.SetRuntimeLibraryOrder([RuntimeLibrary.Cpu, RuntimeLibrary.Cuda]);
        //
        // // This section detects whether the "ggml-base.bin" file exists in our project disk. If it doesn't, it downloads it from the internet
        // if (!File.Exists(modelFileName))
        // {
        //     await DownloadModel(modelFileName, ggmlType);
        // }
        //
        // // This section creates the whisperFactory object which is used to create the processor object.
        // using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");
        //
        // // This section creates the processor object which is used to process the audio file, it uses language `auto` to detect the language of the audio file.
        // await using var processor = whisperFactory.CreateBuilder()
        //     .WithLanguage("auto")
        //     .Build();
        //
        // await using var fileStream = File.OpenRead(wavFileName);
        //
        // // This section processes the audio file and prints the results (start time, end time and text) to the console.
        // await foreach (var result in processor.ProcessAsync(fileStream))
        // {
        //     Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
        // }
    }
    
    private static async Task DownloadModel(string fileName)//, GgmlType ggmlType)
    {
        // Console.WriteLine($"Downloading Model {fileName}");
        // await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
        // await using var fileWriter = File.OpenWrite(fileName);
        // await modelStream.CopyToAsync(fileWriter);
    }
}