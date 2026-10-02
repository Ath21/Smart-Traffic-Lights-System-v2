using System;
using System.Linq;
using System.Timers;
using Messages.Traffic.Light;
using TrafficLightCacheData.Repositories;

namespace TrafficLightControllerStore.Aggregators.Time;

public class TrafficLightTimer : ITrafficLightTimer
{
    private readonly ITrafficLightCacheRepository _cache;
    private readonly int _intersectionId;
    private readonly int _lightId;
    private System.Timers.Timer _timer = new(1000); // 1-second tick

    private Dictionary<string, int> _phases = new();
    private string[] _phaseOrder = { "Green", "Yellow", "Red" };
    private int _currentPhaseIndex;
    private int _remainingTime;

    public TrafficLightTimer(ITrafficLightCacheRepository cache, int intersectionId, int lightId)
    {
        _cache = cache;
        _intersectionId = intersectionId;
        _lightId = lightId;

        _timer.Elapsed += async (s, e) => await TimerTickAsync();
        _timer.AutoReset = true;
    }

    public void Start(TrafficLightControlMessage msg)
    {
        _phases = msg.PhaseDurations ?? new Dictionary<string, int> { { "Green", 40 }, { "Yellow", 5 }, { "Red", 20 } };
        _phaseOrder = _phases.Keys.ToArray();
        int cycleDuration = _phases.Values.Sum();

        // Apply LocalOffsetSec to stagger phases
        int offset = msg.LocalOffsetSec % cycleDuration;
        _currentPhaseIndex = 0;
        _remainingTime = _phases[_phaseOrder[_currentPhaseIndex]];

        // Advance phase according to offset
        while (offset >= _remainingTime)
        {
            offset -= _remainingTime;
            _currentPhaseIndex = (_currentPhaseIndex + 1) % _phaseOrder.Length;
            _remainingTime = _phases[_phaseOrder[_currentPhaseIndex]];
        }

        _remainingTime -= offset;

        if (!_timer.Enabled)
            _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private async Task TimerTickAsync()
    {
        _remainingTime--;

        if (_remainingTime <= 0)
        {
            _currentPhaseIndex = (_currentPhaseIndex + 1) % _phaseOrder.Length;
            _remainingTime = _phases[_phaseOrder[_currentPhaseIndex]];
        }

        string currentPhase = _phaseOrder[_currentPhaseIndex];

        // Update cache
        await _cache.SetCurrentPhaseAsync(_intersectionId, _lightId, currentPhase);
        await _cache.SetRemainingTimeAsync(_intersectionId, _lightId, _remainingTime);
        await _cache.SetCycleProgressAsync(_intersectionId, _lightId, 
            _phases.Values.Take(_currentPhaseIndex).Sum() + (_phases[currentPhase] - _remainingTime));
    }
}
