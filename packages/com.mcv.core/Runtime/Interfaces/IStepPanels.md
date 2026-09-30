# Contract: IStepToolPanel / IStepUiPanel / IStepQuestionPanel

Role: minimal contracts for the three step-panel kinds; conditions look the controller up by name through GlobalControllerMgr and depend on the interface rather than a concrete class.

Fields:
IStepToolPanel.OnToolPressed:event Action<string>  a tool item was pressed (payload = toolId); a condition reacts only when the id equals its own usingId
IStepUiPanel.OnPanelClosed:event Action  the info panel was closed by the user
IStepQuestionPanel.OnQuestionCorrect:event Action  the user answered correctly

Methods:
IStepToolPanel.ShowPanel()  open the tool panel
IStepToolPanel.SetToolDragging(string toolId)  set or clear the dragging tool (hides the dragged tool icon; null ends the drag)
IStepToolPanel.ClosePanel()  close the tool panel (fallback on jump-back or step complete)
IStepUiPanel.ShowData(string uiId)  show the entry whose id equals uiId
IStepUiPanel.ClosePanel()  close the info panel
IStepQuestionPanel.ShowQuestion(string questionId)  show the question whose id equals questionId
IStepQuestionPanel.ClosePanel()  close the question panel

Notes:
- All three extend IController, so they resolve through GlobalControllerMgr.Find("<Name>Controller"); the controller name must match the panel name.
- When no matching panel controller exists yet, the consuming condition logs a warning and skips: it must never block the step flow.
- IStepToolPanel / IStepUiPanel / IStepQuestionPanel are largely unimplemented today; implementers are planned under Steps/.
