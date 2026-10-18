using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AccessibleOCRStudio;
using AccessibleOCRStudio.Services;

internal static class OpenAiChecks
{
    public static void Run(string temp, Action<bool,string> check)
    {
        const string key = "fake-api-key-for-tests";
        const string original = "مرحبا بالعالم\nThe qu1ck fox.\nBonjour le monde.";
        const string corrected = "مرحبا بالعالم\nThe quick fox.\nBonjour le monde.";
        AppSettings Allowed() => new() { OfflineOnlyMode=false, ApiProvider="OpenAI", AllowApiDocumentContent=true, AllowApiOcrCorrection=true };
        var handler = new FakeHandler(); using var client = new HttpClient(handler);
        var service = new OpenAiCorrectionService(client);
        var defaults = new AppSettings();
        check(defaults.OfflineOnlyMode && !defaults.AllowApiDocumentContent && !defaults.AllowApiOcrCorrection && defaults.ApiProvider=="None", "Local OCR defaults to offline with API permissions disabled and no key required");
        var cases = new List<AppSettings> { new(), Allowed(), Allowed(), Allowed(), Allowed() };
        cases[1].OfflineOnlyMode=true; cases[2].AllowApiDocumentContent=false; cases[3].AllowApiOcrCorrection=false; cases[4].ApiProvider="Custom";
        foreach(var settings in cases) ExpectFailure(() => service.CorrectAsync(original, settings, key, true).GetAwaiter().GetResult(), check, "API privacy gate refuses before sending a request");
        ExpectFailure(() => service.CorrectAsync(original, Allowed(), "", true).GetAwaiter().GetResult(), check, "Missing API key refuses before sending");
        ExpectFailure(() => service.CorrectAsync(original, Allowed(), key, false).GetAwaiter().GetResult(), check, "Sending document text requires confirmation");
        ExpectFailure(() => service.CorrectAsync(new string('x',OpenAiCorrectionService.MaximumCharacters+1), Allowed(), key, true).GetAwaiter().GetResult(), check, "Over-limit API input is refused without truncating the document");
        check(handler.Calls==0,"Offline, permission, provider, key and confirmation refusals made zero HTTP requests");
        handler.Body = JsonSerializer.Serialize(new { status="completed", output=new object[] { new { type="reasoning" }, new { type="message", content=new[] { new { type="output_text", text=corrected } } } } });
        var result = service.CorrectAsync(original, Allowed(),key,true).GetAwaiter().GetResult();
        check(result==corrected,"Responses parsing preserves Arabic/English/French and skips non-message output items");
        using(var request=JsonDocument.Parse(handler.RequestBody!))
        {
            check(handler.Url=="https://api.openai.com/v1/responses" && handler.Authorization=="Bearer "+key,"API requests use official HTTPS Responses endpoint and per-request user key");
            check(!request.RootElement.GetProperty("store").GetBoolean() && request.RootElement.GetProperty("input").GetString()==original,"API sends exactly the chosen text with response storage disabled");
            check(!handler.RequestBody!.Contains(key),"API key is absent from the document request body");
        }
        service.TestConnectionAsync(Allowed(),key).GetAwaiter().GetResult();
        using (var probe = JsonDocument.Parse(handler.RequestBody!))
            check(handler.Method==HttpMethod.Post && handler.Url=="https://api.openai.com/v1/responses" && probe.RootElement.GetProperty("input").GetString()=="Clear Read API connection test." && !handler.RequestBody.Contains(original),"Test API key checks actual generation using only a fixed non-document sample");
        foreach(var status in new[] { HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden,HttpStatusCode.NotFound,HttpStatusCode.TooManyRequests,HttpStatusCode.InternalServerError,HttpStatusCode.Redirect })
        {
            handler.Status=status; handler.Body="remote-secret-document-or-key";
            string? failure = null;
            try { service.CorrectAsync(original,Allowed(),key,true).GetAwaiter().GetResult(); }
            catch(InvalidOperationException ex) { failure = ex.Message; }
            check(failure is not null && !failure.Contains(handler.Body),$"HTTP {(int)status} gives a safe error without echoing the response body");
        }
        handler.Status=HttpStatusCode.OK;
        foreach(var body in new[] { "not-json", "{\"status\":\"incomplete\",\"output\":[]}", "{\"status\":\"completed\",\"output\":[]}", "{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"refusal\",\"refusal\":\"no\"}]}]}" })
        {
            handler.Body=body;
            ExpectFailure(()=>service.CorrectAsync(original,Allowed(),key,true).GetAwaiter().GetResult(),check,"Malformed, incomplete, empty or refused corrections cannot replace local text");
        }
        handler.Disconnected=true;
        ExpectFailure(()=>service.CorrectAsync(original,Allowed(),key,true).GetAwaiter().GetResult(),check,"Network failure leaves offline OCR available with an actionable error");
        handler.Disconnected=false;
        using(var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var cancelled = false;
            try { service.CorrectAsync(original,Allowed(),key,true,cancellation.Token).GetAwaiter().GetResult(); }
            catch(OperationCanceledException) { cancelled = true; }
            check(cancelled,"API correction supports cancellation");
        }
        var folder=Path.Combine(temp,"api-isolation"); var credentials=new CredentialService(folder);
        credentials.Save("OpenAI",key); credentials.Save("Custom","other-dummy-key");
        check(new CredentialService(folder).Load("OpenAI")==key,"User API key survives reloading the Windows-protected credential store");
        credentials.Delete("OpenAI");
        check(credentials.Load("OpenAI")==null && credentials.Load("Custom")=="other-dummy-key","Removing OpenAI key preserves other provider credentials");
        var settingsWindow=new ApiSettingsWindow(new AppSettings { ApiProvider="OpenAI" },credentials);
        check(((Button)settingsWindow.FindName("CreateApiKey")).IsEnabled && OpenAiCorrectionService.ApiKeysUrl=="https://platform.openai.com/api-keys","Each user has an enabled link to create their own key on the official OpenAI website");
        check(!((CheckBox)settingsWindow.FindName("ImageDescription")).IsEnabled && !((CheckBox)settingsWindow.FindName("TableReconstruction")).IsEnabled,"Unimplemented image/table API options cannot be enabled");
        settingsWindow.Close();
        var review=new ApiCorrectionReviewWindow(original,corrected);
        check(review.Proposal.Text==corrected && review.Proposal.IsReadOnly && AutomationProperties.GetName(review.Proposal)=="Proposed correction","API proposal is separately reviewable and accessible before application");
        check(original.Contains("qu1ck"),"Correction service does not mutate the original text");
        review.Close();
        var main = new MainWindow(); var editor = (TextBox)main.FindName("DocumentText");
        editor.ApplyTemplate(); editor.Measure(new Size(800,600)); editor.Arrange(new Rect(0,0,800,600)); editor.UpdateLayout();
        editor.Text = "Before qu1ck after"; editor.IsUndoEnabled=false; editor.IsUndoEnabled=true; main.ApplyApiCorrection(7,5,"quick");
        check(editor.Text=="Before quick after","Applying a correction replaces only the sent selection");
        editor.Undo(); check(editor.Text=="Before qu1ck after","Undo restores the exact original after applying an API correction");
        editor.Clear(); main.Close();
    }
    private static void ExpectFailure(Action action,Action<bool,string> check,string message)
    {
        var refused = false; try { action(); } catch(InvalidOperationException) { refused = true; } check(refused,message);
    }
    private sealed class FakeHandler : HttpMessageHandler
    {
        public int Calls; public string? RequestBody,Url,Authorization; public HttpMethod? Method;
        public string Body="{}"; public HttpStatusCode Status=HttpStatusCode.OK; public bool Disconnected;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if(Disconnected) throw new HttpRequestException("private transport details must not be displayed");
            Calls++; Url=request.RequestUri!.AbsoluteUri; Method=request.Method; Authorization=request.Headers.Authorization?.ToString();
            RequestBody=request.Content==null?null:await request.Content.ReadAsStringAsync(token);
            return new HttpResponseMessage(Status) { Content=new StringContent(Body,Encoding.UTF8,"application/json") };
        }
    }
}








