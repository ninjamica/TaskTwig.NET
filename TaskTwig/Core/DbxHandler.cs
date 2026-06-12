using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;

namespace TaskTwig.Core;

public struct DbxCredentials
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class DbxHandler
{
    private const string ApiKey = "7d9hgz3wjirbsrg";

    private readonly string _credentialPath;
    
    private DbxCredentials _credentials;
    private DropboxClient? _dropboxClient;
    
    public bool IsAccountConnected => _dropboxClient is not null;

    public DbxHandler(string dataDirPath)
    {
        _credentialPath = Path.Combine(dataDirPath, "dbx", "credentials.json");
        Directory.CreateDirectory(Path.Combine(dataDirPath, "dbx"));
        
        if (File.Exists(_credentialPath))
        {
            AuthFromStoredKeys();
        }
    }

    public bool AuthFromStoredKeys()
    {
        try
        {
            var json = File.ReadAllText(_credentialPath);
            _credentials = JsonSerializer.Deserialize<DbxCredentials>(json);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            Console.WriteLine("Failed to load credentials.json!");
            Console.WriteLine(ex.Message);
            return false;
        }

        try {
            _dropboxClient = new DropboxClient(_credentials.AccessToken, _credentials.RefreshToken, _credentials.ExpiresAt, ApiKey);
            _WriteAuthKeys();
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or DropboxException)
        {
            Console.WriteLine("Failed to authenticate using stored credentials!");
            Console.WriteLine(ex.Message);
            return false;
        }
    }
    
    private void _WriteAuthKeys()
    {
        var json = JsonSerializer.Serialize(_credentials);
        File.WriteAllText(_credentialPath, json);
    }

    public void AuthFromUrlConsole()
    {
        var oAuthFlow = new PKCEOAuthFlow();
        
        Console.WriteLine($"Auth URL: {GenDbxAuthUrl(oAuthFlow)}");
        Console.Write("Enter auth code: ");
        string? authCode = Console.ReadLine();

        AuthFromCode(oAuthFlow, authCode);
    }

    public void AuthFromCode(PKCEOAuthFlow oAuthFlow, string code)
    {
        var tokenResult = oAuthFlow.ProcessCodeFlowAsync(code, ApiKey).Result;
        
        _dropboxClient = new DropboxClient(tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresAt.Value, ApiKey);
        _credentials.AccessToken = tokenResult.AccessToken;
        _credentials.RefreshToken = tokenResult.RefreshToken;
        _credentials.ExpiresAt = tokenResult.ExpiresAt.Value;
        
        _WriteAuthKeys();
    }

    public Uri GenDbxAuthUrl(PKCEOAuthFlow oAuthFlow)
    {
        return oAuthFlow.GetAuthorizeUri(OAuthResponseType.Code, ApiKey, tokenAccessType:TokenAccessType.Offline);
    }

    public async Task DownloadFileAsync(Stream fileStream, string dbxFilePath)
    {
        if (_dropboxClient is null)
            throw new InvalidOperationException("Dropbox client not initialized!");
        
        var download = await _dropboxClient.Files.DownloadAsync(dbxFilePath).ConfigureAwait(false);
        var downloadStream = await download.GetContentAsStreamAsync().ConfigureAwait(false);
        await downloadStream.CopyToAsync(fileStream);
    }

    public Task<FileMetadata> UploadFileAsync(Stream fileStream, string dbxFilePath)
    {
        if (_dropboxClient is null)
            throw new InvalidOperationException("Dropbox client not initialized!");
        
        return _dropboxClient.Files.UploadAsync(dbxFilePath, mode: WriteMode.Overwrite.Instance, body:fileStream);
    }
    
}