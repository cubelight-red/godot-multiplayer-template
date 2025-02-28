using Godot;
using System.Collections.Generic;

public partial class ServerPlayer : CharacterBody3D
{
    public int MultiplayerID { get; set; } = 0;
    public int Stamp { get; private set; } = 0;

    private Queue<NetMessage.UserInput> _pendingInputs = new();
    private int _lastStampReceived = 0;

    //TODO: this should be dynamic, currently the queue will fill at 4 ticks
    private int _packetWindow = 4;

    public void ProcessPendingCommands()
    {
        if (_pendingInputs.Count <= 0)
            return;

        while (_pendingInputs.Count > _packetWindow)
        {
            var input = _pendingInputs.Dequeue();
            GD.PrintErr($"Server dropping package {input.Stamp} for {MultiplayerID}"); //TODO: this is not very good
        }

        var userInput = _pendingInputs.Dequeue();
        Move(userInput);
    }

    public void PushCommand(NetMessage.UserCommand command)
    {
        foreach (NetMessage.UserInput userInput in command.Commands)
        {
            if (userInput.Stamp == _lastStampReceived + 1)
            {
                _pendingInputs.Enqueue(userInput);
                _lastStampReceived = userInput.Stamp;
            }
        }
    }

    private void Move(NetMessage.UserInput userInput)
    {
        Stamp = userInput.Stamp;

        this.Velocity = PlayerMovement.ComputeMotion(
            this.GetRid(),
            this.GlobalTransform,
            this.Velocity,
            PlayerMovement.InputToDirection(userInput.Keys));

        Position += this.Velocity * (float)PlayerMovement.FRAME_DELTA;
    }
}
