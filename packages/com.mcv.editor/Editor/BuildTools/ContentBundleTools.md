# Contract: ContentBundleTools (+ ClipPlan)

Role: editor pipeline that packs every ProjectClip into its own AssetBundle (Content/clip_{device}) and keeps PackageDB_Master in sync.

Fields:
Providers:IContentProvider[]  the registered providers; array order is the execution order
ClipPlan.clipId:string  the clip id, e.g. clip_contactor
ClipPlan.device:string  the device segment derived from the clip id
ClipPlan.bundleName:string  the bundle name, e.g. Content/clip_contactor
ClipPlan.entries:List<ContentResourceEntry>  the resources collected for this clip

Methods:
RunAll() / BuildOnly() / AuditOnly() / RefreshDatabaseOnly() / CleanLegacy()  the five menu entry points
Run(writeConfig, build, askBeforeBuild)  the single core path every menu calls
BuildPlans(data, errors, extras)  run the providers per clip; a clip without resources emits no bundle
AuditPlans(plans, errors)  duplicate ids across clips and the lower-case bundle file-name segment
GenerateConfigs(plans)  delete stale configs, create or update, self-check broken ones, then sync
BuildBundles(plans) / CleanLegacyArtifacts()  clean legacy directories, then build with sweepStale
ReadProjectData() / ExpectedIdsFromJson() / WarnResiduePackages(expectedIds)  read-only JSON read and residue reporting
BuildReport(plans, errors, extras) / ClipLine(plan) / HeadLines(lines, max) / WithSource(entry, provider)  console and dialog text helpers

Notes:
- Bundle granularity is the ProjectClip: every resource referenced by the clip's four TaskData entries goes into one package, and a clip with no resources is skipped rather than emitted as an empty AssetBundleBuild.
- The config id must be the key actually written in ProjectData.json ({device}_{task}_{resource}); a renamed JSON key silently becomes a "config exists, bundle does not" id.
- The bundle file-name segment must stay lower case, because the runtime lower-cases the last segment while the build writes it verbatim, which ends as a 404 on WebGL and Linux.
- Video never enters a bundle and stays exposed under StreamingAssets/Video.
- Only Sync writes PackageDB_Master and AutoCollect rewrites the whole list, so a dry run goes through the read-only audit; GenerateConfigs deletes stale configs before syncing, while the build-only and refresh-only paths merely warn about residue and never delete.
- Providers is the only registration point for a new content kind; the id rules, config generation, sync, build, audit and legacy cleanup all reuse this flow.
- Configs with a lost script read back as null and are deleted, so the menu must be rerun to rebuild them.
