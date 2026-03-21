using System.Collections.Concurrent;
using System.Net.Sockets;

namespace WebAPI.Judging;

public sealed class JudgeConnectionRegistry
{
    private readonly ConcurrentDictionary<string , TcpClient> _judges = new();

    public bool Register(string judgeId , TcpClient client)
    {
        if ( _judges.TryGetValue(judgeId , out var oldClient) )
        {
            try { oldClient.Close(); } catch { }
            _judges.TryRemove(judgeId , out _);
        }

        return _judges.TryAdd(judgeId , client);
    }

    public void Remove(string judgeId)
    {
        if ( _judges.TryRemove(judgeId , out var client) )
        {
            try { client.Close(); } catch { }
        }
    }

    public IReadOnlyCollection<string> GetOnlineJudgeIds()
    {
        return _judges.Keys.OrderBy(x => x).ToList().AsReadOnly();
    }

    public bool TryGet(string judgeId , out TcpClient client)
    {
        return _judges.TryGetValue(judgeId , out client!);
    }

    public bool TryGetAny(out KeyValuePair<string , TcpClient> judge)
    {
        judge = _judges.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(judge.Key) && judge.Value is not null;
    }
}