using IniParser;

namespace IniParserTests;

sealed class EventRecordReceiver : IniStreamParser.IReceiver
{
    private readonly List<string> _events = [];
    public void StartSection(ReadOnlySpan<char> sectionName) => _events.Add($"StartSection({sectionName})");
    public void EndSection(ReadOnlySpan<char> sectionName) => _events.Add($"EndSection({sectionName})");
    public void ReadValue(ReadOnlySpan<char> sectionName, ReadOnlySpan<char> key, ReadOnlySpan<char> value) => 
        _events.Add($"ReadValue({sectionName}, {key}, {value})");
    public void Error(int position, int length, string errorMessage) => throw new ParserException(position, length, errorMessage);
    public static (EventRecordReceiver, Func<EventRecordReceiver, string[]>) Factory() => (new EventRecordReceiver(), i => i._events.ToArray());
}