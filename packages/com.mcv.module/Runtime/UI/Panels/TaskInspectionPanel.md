# Contract: TaskInspectionPanel

Role: inspection/measurement panel (View); a mouse-following tip box plus a fixed operation record. When to open and what text to show are decided by TaskInspectionController.

Fields:
floatParent:RectTransform  tip box root that follows the mouse (usually TipsFloat)
tipsText:Text  text inside the tip box
tipsOffset:Vector2  offset from the mouse in canvas units; default (0, 80)
opRecordText:Text  fixed operation record text; shows only the latest line
opRecordDefaultText:string  placeholder shown when there is no record; default "暂无操作记录"
m_TipsTextComp / m_OpRecordTextComp:TextComponent  两个文本节点上的组件，在 Awake 缓存；TMP 形态下组件会卸载节点上的 Legacy Text（字段随即成"假 null"），静态入口会静默 no-op，只有持有组件才读写得到；判空一律"字段 == null && 组件 == null"
isTipsShow:bool  whether the tip box is currently following the mouse

Methods:
ShowTips(text)  activate the box first (the TMP swap starts from that activation's OnEnable), write the text through the cached component, then RequestLayoutRebuild(floatParent) and place the box under the mouse immediately
CloseTips()  hide the tip box
SetOpRecord(line)  overwrite the latest line; skips when equal or empty
ClearOpRecord()  write the placeholder text back (not an empty string)
RebuildOpRecordLayout()  resolve the record node via TextComponent.NodeOf, then RequestLayoutRebuild on its parent
FollowMouse()  convert the mouse screen position to the box's parent local space and move the box

Notes:
- Both layout refreshes go through PanelBase.RequestLayoutRebuild (a frame later, depth-descending): the tip box and the record box are parent LayoutGroups whose children own the ContentSizeFitters, so a same-frame ForceRebuildLayoutImmediate measures the not-yet-assembled text and only the next show looks right.
- Place with localPosition, never anchoredPosition: anchoredPosition is relative to the anchor reference point and breaks if the anchor changes.
- Do not write Mouse.current.position straight into the position: the Canvas is Scale With Screen Size plus 1920x1080, so pixels are not canvas units.
- ClearOpRecord must write the placeholder rather than "": empty text collapses the text box background and layout.
- SetOpRecord skips an identical line on purpose: a failed probe snaps back and the record should stay unchanged.
- floatParent must be the node that follows the mouse, not a child inside a layout group (a VerticalLayoutGroup would snap it back).
