# Contract: BundleBuilder (+ BuildOutcome)

Role: the single shared-kernel routine turning an AssetBundleBuild[] into on-disk bundles under {outputRoot}/{dirName}/, with two fixed rules: Temp staging (build into Temp/MCV_*, copy only the bundle body without .manifest into StreamingAssets, then delete Temp) and an optional stale sweep.

Fields:
BuildOutcome.Ok:bool  manifest non-null and no missing bodies
BuildOutcome.BundleCount:int  number of bodies copied successfully
BuildOutcome.TotalBytes:long  total bytes copied
BuildOutcome.Lines:List<string>  per-bundle report lines
BuildOutcome.Missing:List<string>  bundle names whose Temp product is missing (build failed)
BuildOutcome.Removed:List<string>  asset paths swept by sweepStale

Methods:
Build(outputRoot, dirName, tempRoot, builds, sweepStale, logTag)  single-directory build: build and copy the bodies into {outputRoot}/{dirName}; sweep only when ok and nothing missing; always delete tempRoot and refresh once; Ok=false for null or empty builds
Build(outputRoot, tempRoot, builds, sweepStale, logTag)  multi-directory overload: ONE BuildAssetBundles call for every build, each body copied to {outputRoot}/{that bundle's directory segment}/, then a per-directory stale sweep carrying only that directory's own bundle file names
BundleDirSegment(bundleName)  private: the directory segment of a bundle name (UI/ui -> UI); null unless it is exactly "dir/file"
SweepStale(targetDir, keepFileNames)  delete files (with their .meta) not in keepFileNames, case-insensitively; returns the removed asset paths

Notes:
- Assets/Editor/Common is the shared kernel: this is the only bundle-copy path, never fork a second routine into the Content or CameraBg tools.
- Path convention: outputRoot is Assets/StreamingAssets, dirName is the sub-segment (Content, CameraBg) and a bundle name equals its StreamingAssets-relative path, so it may contain directory segments.
- Write timing: build into the Temp root (Temp/MCV_{name}, never imported), copy with File.Copy overwrite, then delete tempRoot and call AssetDatabase.Refresh() once at the end.
- The stale sweep is gated behind a successful, complete build so a failed build never mutates the (possibly last-good) target directory.
- logTag prefixes all debug output; literal tags, messages and paths must never change.
- The multi-directory overload exists because of the same-build rule: Unity's "an asset explicitly assigned to a bundle is no longer copied" only holds WITHIN one BuildAssetBundles call. Measured on TipsPanel.prefab (nests a base prefab referencing SIMHEI.TTF): building the panel bundle and the font bundle separately yields a 6187 KB panel bundle with the font copied inside, while declaring both in one call yields a 152 KB panel bundle plus a 6033 KB font bundle (dependency only). So the runner must be able to hand one call a set of bundles that land in different directories.
- Each bundle body lands under its own directory segment (UI/ui -> {outputRoot}/UI/, Fonts/font -> {outputRoot}/Fonts/); the sweep is then done per directory with only that directory's own file names, because sweeping one directory with the union of all bundle names would delete another pipeline's output.
- The single-directory overload is unchanged and still used by ContentBundleTools, so the content pipeline's semantics (one directory, one sweep over that directory's file names) are byte-for-byte the same.
- A build whose bundle name is not exactly "dir/file" is reported as missing with a readable error rather than guessing a directory.
