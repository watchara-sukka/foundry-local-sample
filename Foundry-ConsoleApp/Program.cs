using System.ClientModel;
using Microsoft.AI.Foundry.Local;

using OpenAI;
using OpenAI.Chat;

public class TestApp
{
    public static async Task Main(string[] args)
    {
        var app = new TestApp(); // Create an instance of TestApp  

        Console.WriteLine(new string('=', 80)); // Separator for clarity
        Console.WriteLine("Show catalog integration...");//ทดสอบแสดงรายการmodel ใน catalog
        await app.ShowCatalog();

        Console.WriteLine(new string('=', 80)); // Separator for clarity
        Console.WriteLine("Testing cache operations...");
        await app.TestCacheOperations(); // Call the instance method

        Console.WriteLine(new string('=', 80)); // Separator for clarity
        Console.WriteLine("Connect Model using  OpenAI integration (from stopped service)...");
        using var manager = new FoundryLocalManager();
        if (manager != null)
        {
            //ตรวจสอบว่าถ้ามี model ทำงานอยู่ให่ทำการหยุด
            await manager.StopServiceAsync();
        }
        //ทดสอบเรียนใช้ model และป้อนคำถาม
        await app.ConnectModel("qwen2.5-coder-1.5b");

        Console.WriteLine(new string('=', 80)); // Separator for clarity
        Console.WriteLine("Testing service operations");
        await app.ShowService(); // Call the instance method
        //ทดสอบ load/unload model
        Console.WriteLine(new string('=', 80)); // Separator for clarity
        Console.WriteLine("Testing model (un)loading");
        await app.LoadUnloadModel("qwen2.5-coder-1.5b"); // Call the instance method
        //ทดสอบ download ai model from internet
        Console.WriteLine(new string('=', 80)); // Separator for clarity
        Console.WriteLine("Testing downloading");
        await app.DownloadModel("qwen2.5-coder-1.5b"); // Call the instance method
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(true);
    }

    //fuction ในการแสดงรายชื่ model ใน catalog
    private async Task ShowCatalog()
    // 
    {
        using var manager = new FoundryLocalManager();
        foreach (var m in await manager.ListCatalogModelsAsync())
        {
            Console.WriteLine($"Model: {m.Alias} ({m.ModelId})");
        }
    }

    //ตรวจสอบตำแหน่งการจัดเก็บ model ที่อยู่บนเครืี่องและตรวจสอบว่ามี model ไหนอยู่บนเครื่องบ้าง
    private async Task TestCacheOperations()
    {
        using var manager = new FoundryLocalManager();
        // แสดงตำแหน่งเก็บไฟล์ model
        Console.WriteLine($"Model cache location at {await manager.GetCacheLocationAsync()}");
        // แสดงรายการ model ที่อยู่ใน local
        var models = await manager.ListCachedModelsAsync();
        Console.WriteLine($"Found {models.Count} models in the cache:");
        foreach (var m in models)
        {
            Console.WriteLine($"Model: {m.Alias} ({m.ModelId})");
        }
    }

    //เชื่อมต่อกับ model
    private async Task ConnectModel(string aliasOrModelId)
    {

        var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId);
        //เลือก model จาก model ที่กำหนด 
        var model = await manager.GetModelInfoAsync(aliasOrModelId);
        var key = new ApiKeyCredential(manager.ApiKey); //ดึงค่า apikey จาก model
        //สรา้ง OpenAI client โดยเลือก Endpoint จาก model ที่เลือกไว้
        var client = new OpenAIClient(key, new OpenAIClientOptions
        {
            Endpoint = manager.Endpoint
        });
        //OpenAI Client เชื่อมต่อกับ Model
        var chatClient = client.GetChatClient(model?.ModelId);
        //เก็บคำตอบเมื่อถาม model ว่า "Why is the sky blue"
        CollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreaming("Why is the sky blue'");

        Console.Write($"[ASSISTANT]: ");
        foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                Console.Write(completionUpdate.ContentUpdate[0].Text);
            }
        }
    }
    //load or unload ai model
    private async Task LoadUnloadModel(string aliasOrModelId)
    {
        using var manager = new FoundryLocalManager();
        // Load a model
        var model = await manager.LoadModelAsync(aliasOrModelId);
        Console.WriteLine($"Loaded model: {model.Alias} ({model.ModelId})");
        // Unload the model
        await manager.UnloadModelAsync(aliasOrModelId);
        Console.WriteLine($"Unloaded model: {model.Alias} ({model.ModelId})");
    }

    //download ai model form internet
    private async Task DownloadModel(string aliasOrModelId)
    {
        using var manager = new FoundryLocalManager();

        // Download a model
        var model = await manager.DownloadModelAsync(aliasOrModelId, force: true);

        // test that the model can be loaded
        Console.WriteLine($"Downloaded model: {model!.Alias} ({model.ModelId})");
    }
    //แสดงรายละเอียด service ที่ทำงานอยู่
    private async Task ShowService()
    {
        using var manager = new FoundryLocalManager();
        await manager.StartServiceAsync();
        // Print out whether the service is running
        Console.WriteLine($"Service running (should be true): {manager.IsServiceRunning}");
        // Print out the service endpoint and API key
        Console.WriteLine($"Service Uri: {manager.ServiceUri}");
        Console.WriteLine($"Endpoint {manager.Endpoint}");
        Console.WriteLine($"ApiKey: {manager.ApiKey}");
        // stop the service
        await manager.StopServiceAsync();
        Console.WriteLine($"Service stopped");
        Console.WriteLine($"Service running (should be false): {manager.IsServiceRunning}");
    }
    
}
