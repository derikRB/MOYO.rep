using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;


[ApiController]
[Route("api/admin/faq")]
public class FaqController : ControllerBase
{
    private readonly IFaqService _svc;
    public FaqController(IFaqService svc) => _svc = svc;

    [HttpGet]
    public Task<List<FaqItemDto>> GetAll() => _svc.GetAllAsync();

    [HttpGet("{id}")]
    public Task<FaqItemDto> Get(int id) => _svc.GetByIdAsync(id);

    [HttpPost]
    public async Task<ActionResult<FaqItemDto>> Create([FromBody] FaqItemDto dto)
    {
        var created = await _svc.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.FaqId }, created);
    }

    [HttpPost("rasa/export-and-retrain")]
    public async Task<IActionResult> ExportAndRetrain()
    {
        // 1. Export FAQs to nlu.yml
        var faqs = await _svc.GetAllAsync();
        var sb = new StringBuilder();
        sb.AppendLine("version: \"3.1\"");
        sb.AppendLine("nlu:");
        foreach (var faq in faqs)
        {
            string intentName = "faq_" + faq.FaqId;
            sb.AppendLine($"- intent: {intentName}");
            sb.AppendLine("  examples: |");
            sb.AppendLine($"    - {faq.QuestionVariant}");
        }
        var baseDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..")); // adjust if needed
        var nluPath = Path.Combine(solutionDir, "chatbot", "data", "nlu.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(nluPath));
        System.IO.File.WriteAllText(nluPath, sb.ToString());

        // 2. Run Rasa train
        string rasaFolder = Path.Combine(solutionDir, "chatbot");
        string pythonEnvScripts = Path.Combine(solutionDir, "chatbot_env", "Scripts");
        string activateCmd = $"cd /d \"{rasaFolder}\" && call \"{pythonEnvScripts}\\activate\" && rasa train";
        string output = RunCommand("cmd.exe", $"/c {activateCmd}", rasaFolder);

        // 3. Kill and restart Rasa run and actions servers (if running manually)
        // Kill old ones (processes named "python" or "rasa")
        KillProcess("rasa");
        KillProcess("python"); // If you use "python rasa run ..."

        // (Optional) Start Rasa run and actions servers again
        // Note: For true production, use a supervisor like pm2, systemd, or Windows service for process management
        // The following is for simple dev usage:
        RunCommand("cmd.exe", $"/c start cmd.exe /k \"cd /d {rasaFolder} && call ..\\chatbot_env\\Scripts\\activate && rasa run actions\"", rasaFolder);
        RunCommand("cmd.exe", $"/c start cmd.exe /k \"cd /d {rasaFolder} && call ..\\chatbot_env\\Scripts\\activate && rasa run\"", rasaFolder);

        return Ok(new { message = "Exported, retrained, and attempted server restart.", rasaOutput = output });
    }


    [HttpPut("{id}")]
    public Task<FaqItemDto> Update(int id, [FromBody] FaqItemDto dto)
    {
        dto.FaqId = id;
        return _svc.UpdateAsync(dto);
    }

    [HttpDelete("{id}")]
    public Task Delete(int id) => _svc.DeleteAsync(id);

    [HttpGet("search")]
    public Task<List<FaqItemDto>> Search([FromQuery] string q) => _svc.SearchAsync(q);

    [HttpGet("rasa/export-nlu")]
    public async Task<IActionResult> ExportToNluYml()
    {
        var faqs = await _svc.GetAllAsync();
        var sb = new StringBuilder();
        sb.AppendLine("version: \"3.1\"");
        sb.AppendLine("nlu:");

        // Each FAQ gets a unique intent, or use a shared "faq" intent for all Q/As.
        foreach (var faq in faqs)
        {
            string intentName = "faq_" + faq.FaqId;
            sb.AppendLine($"- intent: {intentName}");
            sb.AppendLine("  examples: |");
            sb.AppendLine($"    - {faq.QuestionVariant}");
        }

        // Find the root of your solution (relative to bin/Debug/...)
        // You may need more or fewer '..' depending on folder depth.
        var baseDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
        var nluPath = Path.Combine(solutionDir, "chatbot", "data", "nlu.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(nluPath)); // ← ensures folder exists!
        System.IO.File.WriteAllText(nluPath, sb.ToString());

        return Ok(new { message = "nlu.yml exported", count = faqs.Count });
    }

    // Helper to run a command and return its output (for Windows). to retrain the bot
    private string RunCommand(string fileName, string arguments, string workingDir = null)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? AppContext.BaseDirectory
        };
        var process = System.Diagnostics.Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return output + "\n" + error;
    }

    // Helper to kill processes by name. this kill and restart the rasa servers: rasa run and
    private void KillProcess(string processName)
    {
        foreach (var proc in System.Diagnostics.Process.GetProcessesByName(processName))
        {
            try { proc.Kill(); }
            catch { }
        }
    }

}
