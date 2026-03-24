namespace Application.Abstractions.Outbound.Extensions
{
    public interface IObjectStorage
    {
        Task<string> PutAsync(string bucket, string key, Stream content, CancellationToken ct);
        //  giả dụ ae ưng ngầu oách thì đó vps thì con khác nhưng cloud đồ thì xài của google hoặc aws
    }
}
