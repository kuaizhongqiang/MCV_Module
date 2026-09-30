# Contract: UserDataSO

Role: user data SO; exports StreamingAssets/Data/UserData.json.

Fields:
data:UserData  the user data, default new UserData()

Methods:
Export()  [ContextMenu] override -> ExportData(data)

Notes:
- The data field lives on this concrete SO because Unity cannot serialize a field typed by a generic type parameter.
- The ContextMenu string is Chinese and must stay unchanged.
