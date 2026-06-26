using Logic.Scripts.Extensions;
using Logic.Scripts.Services.CommandFactory;
using System.Threading;
using UnityEngine;

public class PortalController : IPortalController {
    private readonly ICommandFactory _commandFactory;
    private bool _portalTransitionInProgress;

    public PortalController(ICommandFactory commandFactory) {
        _commandFactory = commandFactory;
    }

    public void SetUpPortals(PortalView[] portalViews) {
        if (portalViews.IsNullOrEmpty()) {
            return;
        }
        foreach (PortalView portalView in portalViews) {
            portalView.Setup(OnTriggerEnterAction);
        }
    }
    private async void OnTriggerEnterAction(PortalView portal, int levelToEnter) {
        if (_portalTransitionInProgress) return;
        _portalTransitionInProgress = true;
        try {
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            await _commandFactory.CreateCommandAsync<PortalEnterCommand>().SetData(new PortalEnterCommandData(levelToEnter)).Execute(cancellationTokenSource);
        }
        finally {
            _portalTransitionInProgress = false;
        }
    }
}
