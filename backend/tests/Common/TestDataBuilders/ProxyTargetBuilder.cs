using AugmentService.Core.Entities;
using AutoFixture;

namespace Common.TestDataBuilders;

public class ProxyTargetBuilder
{
    private readonly Fixture _fixture = new();
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Proxy";
    private string _baseUrl = "https://api.example.com";
    private bool _isActive = true;
    private int _timeoutSeconds = 30;

    public ProxyTargetBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ProxyTargetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProxyTargetBuilder WithBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    public ProxyTargetBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public ProxyTargetBuilder WithTimeout(int timeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
        return this;
    }

    public ProxyTargetBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public ProxyTarget Build() => new()
    {
        Id = _id,
        Name = _name,
        BaseUrl = _baseUrl,
        IsActive = _isActive,
        TimeoutSeconds = _timeoutSeconds
    };

    public static implicit operator ProxyTarget(ProxyTargetBuilder b) => b.Build();
}
