# Unity-Timetrial-Ghost
Time trial ghosts for any kind of speed running game.



### Instructions

#### Example
You can open the demo scene for a quick example. You'll need to drop in some kind of character. [You can use my 2D Controller if you'd like](https://www.patreon.com/tarodev).

#### Step by step
Create an instance of the ReplaySystem:
```cs
private ReplaySystem _system;
private void Awake() => _system = new ReplaySystem(this);
```

To begin recording:
```cs
_system.StartRun(_recordTarget, _captureEveryNFrames);
```

To stop recording:
```cs
_system.FinishRun();
```

To play a recording:
```cs
_system.PlayRecording(RecordingType.Best, Instantiate(_ghostPrefab)); // The ghost should be a very basic prefab without colliders or rigidbodies. See the demo scene for an example.
```

### Leave a ⭐ if you found it helpful!
