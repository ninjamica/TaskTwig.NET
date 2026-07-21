using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

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
            var dbxClientConfig = new DropboxClientConfig { HttpClient = new HttpClient(new SocketsHttpHandler()) };
            _dropboxClient = new DropboxClient(_credentials.AccessToken, 
                                               _credentials.RefreshToken, 
                                               _credentials.ExpiresAt, 
                                               ApiKey, dbxClientConfig);
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
        string authCode = Console.ReadLine() ?? "";

        AuthFromCode(oAuthFlow, authCode);
    }

    public void AuthFromCode(PKCEOAuthFlow oAuthFlow, string code)
    {
        var tokenResult = oAuthFlow.ProcessCodeFlowAsync(code, ApiKey).Result;

        var dbxClientConfig = new DropboxClientConfig { HttpClient = new HttpClient(new SocketsHttpHandler()) };
        _dropboxClient = new DropboxClient(tokenResult.AccessToken, 
                                           tokenResult.RefreshToken, 
                                           tokenResult.ExpiresAt.Value, 
                                           ApiKey, dbxClientConfig);
        
        _credentials.AccessToken = tokenResult.AccessToken;
        _credentials.RefreshToken = tokenResult.RefreshToken;
        _credentials.ExpiresAt = tokenResult.ExpiresAt.Value;
        
        _WriteAuthKeys();
    }

    public Uri GenDbxAuthUrl(PKCEOAuthFlow oAuthFlow)
    {
        return oAuthFlow.GetAuthorizeUri(OAuthResponseType.Code, ApiKey, tokenAccessType:TokenAccessType.Offline);
    }

    public async Task<Stream> DownloadContentStreamAsync(string dbxFilePath)
    {
        if (_dropboxClient is null)
            throw new InvalidOperationException("Dropbox client not initialized!");
        
        var download = await _dropboxClient.Files.DownloadAsync(dbxFilePath);
        return await download.GetContentAsStreamAsync();
    }

    public async Task DownloadFileAsync(Stream fileStream, string dbxFilePath)
    {
        if (_dropboxClient is null)
            throw new InvalidOperationException("Dropbox client not initialized!");

        try
        {
            var download = await _dropboxClient.Files.DownloadAsync(dbxFilePath);
            await using var downloadStream = await download.GetContentAsStreamAsync();
            await downloadStream.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            fileStream.Close();
        }
    }

    public async Task<FileMetadata> UploadFileAsync(Stream fileStream, string dbxFilePath)
    {
        if (_dropboxClient is null)
            throw new InvalidOperationException("Dropbox client not initialized!");
        
        var metadata = await _dropboxClient.Files.UploadAsync(dbxFilePath, mode: WriteMode.Overwrite.Instance, body:fileStream);
        
        fileStream.Close();
        return metadata;
    }

    public async Task<String> GetAccountName()
    {
        if (_dropboxClient is null)
            throw new InvalidOperationException("Dropbox client not initialized!");

        // var asyncResult = _dropboxClient.Users.BeginGetCurrentAccount(_ => {});
        // var account = _dropboxClient.Users.EndGetCurrentAccount(asyncResult);
        // return account.Name.DisplayName;
        return (await _dropboxClient.Users.GetCurrentAccountAsync()).Name.DisplayName;
    }
    
}