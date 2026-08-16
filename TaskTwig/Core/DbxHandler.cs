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
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }

    internal DbxCredentials(string accessToken, string refreshToken, DateTime expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }
}

public class DbxHandler
{
    private const string ApiKey = "7d9hgz3wjirbsrg";

    private readonly string _credentialPath;
    
    private DbxCredentials? _credentials;
    private DropboxClient? _dropboxClient;
    
    public bool IsAccountConnected => _dropboxClient is not null;

    public DbxHandler(string dataDirPath)
    {
        _credentialPath = Path.Combine(dataDirPath, "dbx", "credentials.json");
        Directory.CreateDirectory(Path.Combine(dataDirPath, "dbx"));
    }

    public async Task<bool> AuthFromStoredKeys()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_credentialPath);
            _credentials = JsonSerializer.Deserialize<DbxCredentials>(json);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Credentials file not found, starting with no account");
            return false;
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            Console.WriteLine("Failed to load credentials.json!");
            Console.WriteLine(ex.Message);
            return false;
        }

        try {
            if (_credentials is { } credentials)
            {
                var dbxClientConfig = new DropboxClientConfig { HttpClient = new HttpClient(new SocketsHttpHandler()) };
                _dropboxClient = new DropboxClient(credentials.AccessToken, credentials.RefreshToken, 
                                                   credentials.ExpiresAt, ApiKey, dbxClientConfig);
                _WriteAuthKeys();
                return true;
            }
            else
            {
                return false;
            }
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
        if (_credentials is { } credentials)
        {
            var json = JsonSerializer.Serialize(credentials);
            File.WriteAllText(_credentialPath, json);
        }
    }

    public void AuthFromUrlConsole()
    {
        var (uri, oAuthFlow) = GenDbxAuthUrl();
        Console.WriteLine($"Auth URL: {uri}");
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

        _credentials = new DbxCredentials(tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresAt.Value);
        
        _WriteAuthKeys();
    }

    public (Uri, PKCEOAuthFlow) GenDbxAuthUrl()
    {
        var oAuthFlow = new PKCEOAuthFlow();
        return (oAuthFlow.GetAuthorizeUri(OAuthResponseType.Code, ApiKey, tokenAccessType: TokenAccessType.Offline),
            oAuthFlow);
    }

    public async Task Logout()
    {
        if (_dropboxClient is not null)
        {
            await _dropboxClient.Auth.TokenRevokeAsync();
            _dropboxClient.Dispose();
            File.Delete(_credentialPath);
            
            _dropboxClient = null;
            _credentials = null;
        }
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

    public async Task<string?> GetAccountName()
    {
        var account = _dropboxClient != null ? await _dropboxClient.Users.GetCurrentAccountAsync() : null;
        return account?.Name.DisplayName;
    }
    
}