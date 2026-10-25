using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Users;
using WeakEvent;

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

public class DbxAccountChangedEventArgs(bool isAccountConnected, string? accountName, string? accountPhotoUri) : EventArgs
{
    public bool IsAccountConnected { get; } = isAccountConnected;
    public string? AccountName { get; } = accountName;
    public string? AccountPhotoUri { get; } = accountPhotoUri;
}

public class DbxHandler
{
    private const string ApiKey = "7d9hgz3wjirbsrg";
    
    public readonly DropboxClientConfig DbxClientConfig = new() { HttpClient = new HttpClient(new SocketsHttpHandler()) };

    private readonly string _credentialPath;
    
    private DbxCredentials? _credentials;

    private DropboxClient? _dropboxClient;

    private FullAccount? DbxAccount
    {
        get;
        set
        {
            field = value;
            _accountChangedEventSource.Raise(this, new DbxAccountChangedEventArgs(IsAccountConnected, GetAccountName(), GetAccountPhotoUri()));
        }
    }
    
    public bool IsAccountConnected => DbxAccount is not null;

    private readonly WeakEventSource<DbxAccountChangedEventArgs> _accountChangedEventSource = new();
    public event EventHandler<DbxAccountChangedEventArgs> AccountChanged
    {
        add =>  _accountChangedEventSource.Subscribe(value);
        remove =>  _accountChangedEventSource.Unsubscribe(value);
    }

    
    public DbxHandler(string dataDirPath)
    {
        _credentialPath = Path.Combine(dataDirPath, "dbx", "credentials.json");
        Directory.CreateDirectory(Path.Combine(dataDirPath, "dbx"));
    }

    public async Task<bool> AuthFromStoredKeys()
    {
        DbxAccount = null;
        
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

        try
        {
            if (_credentials is { } credentials)
            {
                _dropboxClient = new DropboxClient(credentials.AccessToken, credentials.RefreshToken, 
                                                   credentials.ExpiresAt, ApiKey, DbxClientConfig);
                _WriteAuthKeys();
                
                DbxAccount = await _dropboxClient.Users.GetCurrentAccountAsync();
                return true;
            }

            return false;
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

    public async Task AuthFromCode(PKCEOAuthFlow oAuthFlow, string code)
    {
        var tokenResult = await oAuthFlow.ProcessCodeFlowAsync(code, ApiKey);

        _dropboxClient = new DropboxClient(tokenResult.AccessToken, 
                                          tokenResult.RefreshToken, 
                                          tokenResult.ExpiresAt!.Value, 
                                          ApiKey, DbxClientConfig);

        _credentials = new DbxCredentials(tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresAt.Value);

        DbxAccount = await _dropboxClient.Users.GetCurrentAccountAsync();
        
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
            DbxAccount = null;
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

    public string? GetAccountName()
    {
        return DbxAccount?.Name.DisplayName;
    }

    public string? GetAccountPhotoUri()
    {
        return DbxAccount?.ProfilePhotoUrl;
    }
    
}