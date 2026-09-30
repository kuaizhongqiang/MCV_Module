# Contract: ResultSummitPanel (+ ResultLabelStruct)

Role: score preview view; eleven label/value rows plus the record text and the submit / close buttons. Pure display, no business logic.

Fields:
labelRoot:Transform  parent of the eleven row instances
recordContentText:Text  experiment step record text
m_RecordContentTextComp:TextComponent  记录文本节点上的组件，在 Awake 缓存；TMP 形态下组件会卸载节点上的 Legacy Text（字段随即成"假 null"，属正常状态，不能当"引用缺失"处理），静态入口会静默 no-op
submitBtn:Button  submit button
cancelBtn:Button  close button
rowDict:Dictionary<string, ResultLabelStruct>  row name -> row handle

Methods:
Awake()  validate references, bind the buttons, build the rows; caches the record node's TextComponent first, and "recordContentText == null" only counts as missing when the component is missing too (TMP form unloads the Legacy Text on purpose)
OnDestroy()  unbind the buttons, null the events, clear the rows, call base
Show(viewData)  fill the rows from the view data and show; warns for missing and extra rows
Hide()  hide the panel without a scene change
SetRow(rowName, value)  set one row's value; warns for an unknown row
SetRecordText(text)  set the record text
BuildRows()  scan labelRoot children into rowDict by child name, de-duplicating and logging
HandleSubmitClick() / HandleCloseClick()  raise the matching event

Nested type ResultLabelStruct  handle for one label/value row
Fields:
LabelChildIndex / ValueHolderChildIndex:int  child indices of the label Text and the value holder
label / value:string  current label and value text
labelText / valueText:Text  cached Texts
labelTextComp / valueTextComp:TextComponent  行内两个文本节点上的组件，在 Create 里解析 Text 的同一处（同节点）缓存；换过形态后 labelText / valueText 成"假 null"，静态入口静默 no-op，该行的标签与数值就永远刷不出来
Methods:
ResultLabelStruct(labelText, labelTextComp, valueText, valueTextComp)  private constructor caching the two Texts and their components in the same place
Create(row)  parse a row into a handle from its fixed structure; takes each TextComponent from the same node as its Text (the TMP form keeps only the component); null only when neither a Text nor a component is present
SetLabel(text) / SetValue(text)  set and store the text

Notes:
- Rows are matched by object name, not by order: renaming a row or dropping one is logged and breaks matching.
- The row structure is fixed: [0] Label(Text) plus [1] Bg -> ContentText(Text); changing the prefab requires updating the index constants.
- Event flow: menu preview -> GlobalDataMgr.PreviewScore() -> ScoreRecordFormatter.Build -> Show -> OnSubmitClicked / OnCloseClicked.
