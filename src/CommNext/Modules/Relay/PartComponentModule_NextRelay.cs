using BepInEx.Logging;
using CommNext.Utils;
using KSP.Game;
using KSP.Modules;
using KSP.Sim.impl;
using KSP.Sim.ResourceSystem;

namespace CommNext.Modules.Relay;

// ReSharper disable once InconsistentNaming
public class PartComponentModule_NextRelay : PartComponentModule
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.PartComponentModule_NextRelay");

    public override Type PartBehaviourModuleType => typeof(Module_NextRelay);

    /// <summary>
    /// A link to the DataTransmitter
    /// </summary>
    public Data_Transmitter DataTransmitter { get; private set; } = null!;

    private Data_NextRelay _dataRelay = null!;

    // EC management
    private FlowRequestResolutionState _returnedRequestResolutionState;
    private bool _hasOutstandingRequest;
    private bool _lastHasResourcesToOperate;

    public override void OnStart(double universalTime)
    {
        Logger.LogDebug($"OnStart triggered. Vessel '{Part?.PartOwner?.SimulationObject?.Vessel?.Name ?? "<none>"}'");

        if (!DataModules.TryGetByType<Data_NextRelay>(out _dataRelay))
        {
            Logger.LogError("Unable to find a Data_Relay in the PartComponentModule for " + Part.PartName);
            return;
        }

        // Setup the resource request. This will be applied only if the
        // `RelayRequiresPower` setting is true.
        _dataRelay.SetupResourceRequest(resourceFlowRequestBroker);

        Part!.TryGetModuleData<PartComponentModule_DataTransmitter, Data_Transmitter>(out var dataTransmitter);
        DataTransmitter = dataTransmitter;
    }

    public override void OnUpdate(double universalTime, double deltaUniversalTime)
    {
        ResourceConsumptionUpdate(deltaUniversalTime);
        RefreshCommNetIfNecessary();
    }

    /// <summary>
    /// Handles the resource consumption for the relay.
    /// </summary>
    private void ResourceConsumptionUpdate(double deltaTime)
    {
        _lastHasResourcesToOperate = _dataRelay.HasResourcesToOperate;

        // 1. If the relay doesn't require power, we don't need to do anything.
        if (!PluginSettings.RelaysRequirePower.Value || DifficultyUtils.HasInfinitePower)
        {
            // Just set resources to operate to true and return.
            _dataRelay.HasResourcesToOperate = true;
            if (resourceFlowRequestBroker.IsRequestActive(_dataRelay.RequestHandle))
                resourceFlowRequestBroker.SetRequestInactive(_dataRelay.RequestHandle);

            return;
        }

        // 2. If we have an outstanding request, we need to check if it was accepted.
        if (_hasOutstandingRequest)
        {
            _returnedRequestResolutionState =
                resourceFlowRequestBroker.GetRequestState(_dataRelay.RequestHandle);
            _dataRelay.HasResourcesToOperate = _returnedRequestResolutionState.WasLastTickDeliveryAccepted;
        }

        _hasOutstandingRequest = false;

        // 3. We need to align Resource request to Enabled state;
        // If relay is disabled, stop active request.
        // If relay is enabled, trigger request.
        switch (_dataRelay.EnableRelay.GetValue())
        {
            case false when resourceFlowRequestBroker.IsRequestActive(_dataRelay.RequestHandle):
                resourceFlowRequestBroker.SetRequestInactive(_dataRelay.RequestHandle);
                // TODO Not sure about this one.
                _dataRelay.HasResourcesToOperate = false;
                break;

            case true when resourceFlowRequestBroker.IsRequestInactive(_dataRelay.RequestHandle):
                resourceFlowRequestBroker.SetRequestActive(_dataRelay.RequestHandle);
                break;
        }

        if (!_dataRelay.EnableRelay.GetValue()) return;

        // 3.1 If the relay is enabled, we need to check if we have enough resources to operate.
        _dataRelay.RequestConfig.FlowUnits = (double)_dataRelay.RequiredResource.Rate;
        resourceFlowRequestBroker.SetCommands(
            _dataRelay.RequestHandle,
            1.0,
            [_dataRelay.RequestConfig]
        );
        _hasOutstandingRequest = true;
    }

    /// <summary>
    /// Trigger a refresh of the CommNet node if the relay has resources to operate.
    /// </summary>
    private void RefreshCommNetIfNecessary()
    {
        if (_lastHasResourcesToOperate == _dataRelay.HasResourcesToOperate) return;

        Logger.LogDebug(
            $"Relay '{Part.PartOwner.DisplayName}' has resources to operate: {_dataRelay.HasResourcesToOperate}");

        Part.PartOwner.SimulationObject.Telemetry.RefreshCommNetNode();
        _lastHasResourcesToOperate = _dataRelay.HasResourcesToOperate;
    }
}