# Contract: DefaultPackageConfigSO

Role: default (direct local reference) package config; routes the resource through Resources or serialised references, load key = id.

Methods:
GetLoadKey()  -> id

Notes:
- Must stay in its own file named after the class: Unity only generates a MonoScript for a class whose name equals the file name (see AAPackageConfigSO), otherwise CreateAsset yields m_Script = 0.
