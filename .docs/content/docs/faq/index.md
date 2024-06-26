---
title: Frequently Asked Questions
weight: 2
---

# Frequently Asked Questions {#faq}

Here is the list of frequently asked questions about Avatar Optimizer.
If you have some other questions, please feel free to ask on the [GitHub Discussions] or on [Fediverse].
I'm usually use Japanese, but you can ask me with English.

## The behavior or appearance of the avatar is changed when using `AAO Trace and Optimize` component {#avatar-behavior-or-appearance-changed-when-using-aao-trace-and-optimize-component}

If the behavior or appearance of the avatar is changed when using `AAO Trace and Optimize` component, it is always a bug unless you depend on bugs in other components of Avatar Optimizer.

Please report it on [GitHub Issues], [misskey][Fediverse] (mastodon), [Twitter], etc.

## Mesh is invisible even though it is in the field of view {#mesh-is-invisible-even-though-it-is-in-the-field-of-view}

This problem is often due to forgetting to specify the `Root Bone` of the Skinned Mesh Renderer.

[`AAO Merge Skinned Mesh`] component does not automatically set the `Root Bone`, so you need to manually set the `Root Bone` of the Merged Mesh[^merged-mesh].

If you are using [Modular Avatar], you can add [`MA Mesh Settings`] component to the root of the avatar to set the `Root Bone` and `Bounds` for the whole avatar.

## The brightness of the meshes merged with `AAO Merge Skinned Mesh` component is different from other meshes {#the-brightness-of-the-meshes-merged-with-aao-merge-skinned-mesh-component-is-different-from-other-meshes}

This problem is often due to forgetting to specify the `Anchor Override` of the merged meshes.\
[`AAO Merge Skinned Mesh`] component does not automatically set the `Anchor Override`, so you need to manually set the `Anchor Override` of the Merged Mesh[^merged-mesh].

If you are using [Modular Avatar], you can add [`MA Mesh Settings`] component to the root of the avatar to set the `Anchor Override` for the whole avatar.

## Material property animations conflict when using `AAO Merge Skinned Mesh` component {#material-property-animations-conflict-when-using-aao-merge-skinned-mesh-component}

This problem is a known bug and is currently expected to conflicts.

When merging meshes with animated material propeeries, be careful not to conflict.
If there is a conflict, a warning will be displayed, so please check the warning.

Issue of this problem: [#340](https://github.com/anatawa12/AvatarOptimizer/issues/340)

## Material slot animations conflict when using `AAO Merge Skinned Mesh` component {#material-slot-animations-conflict-when-using-aao-merge-skinned-mesh-component}

`AAO Merge Skinned Mesh` component will merge material slots using the same material of the merge target meshes by default.
This will also merge animated material slots.

If you have some material slots which will be replaced differently with animation, you should un-check `Merge` of `Merge Materials` of `AAO Merge Skinned Mesh` component.

## BlendShape animations conflict when using `AAO Merge Skinned Mesh` component {#blendshape-animations-conflict-when-using-aao-merge-skinned-mesh-component}

This problem is a known bug and is currently expected to conflicts.

When merging meshes with animated BlendShapes, be careful not to conflict.
If there is a conflict, a warning will be displayed, so please check the warning.

Issue of this problem: [#568](https://github.com/anatawa12/AvatarOptimizer/issues/568)

## I want to support the development of Avatar Optimizer {#i-want-to-support-the-development-of-avatar-optimizer}

If you want to support the development of Avatar Optimizer, feedback on [GitHub Discussions], bug reports, feature requests, etc. on [GitHub Issues], and pull requests are welcome.

Issues with [good first issue] are relatively easy to implement. It is recommended for your first pull request.
Also, issues with [help wanted] are ones that lack developers or information. Your participation in discussions and development would be appreciated.

I also accept financial support on [GitHub Sponsors] and [Booth].

[Fediverse]: https://misskey.niri.la/@anatawa12
[GitHub Discussions]: https://github.com/anatawa12/AvatarOptimizer/discussions
[GitHub Issues]: https://github.com/anatawa12/AvatarOptimizer/issues/new/choose
[`AAO Merge Skinned Mesh`]: ../reference/merge-skinned-mesh/
[Modular Avatar]: https://modular-avatar.nadena.dev/ja/
[`MA Mesh Settings`]: https://modular-avatar.nadena.dev/ja/docs/reference/mesh-settings
[Twitter]: https://twitter.com/anatawa12_vrc
[GitHub Sponsors]: https://github.com/sponsors/anatawa12
[Booth]: https://anatawa12.booth.pm/items/4885109
[good first issue]: https://github.com/anatawa12/AvatarOptimizer/labels/good%20first%20issue
[help wanted]: https://github.com/anatawa12/AvatarOptimizer/labels/help%20wanted

[^merged-mesh]: Merged Mesh is a Skinned Mesh Renderer which is attached along with `AAO Merge Skinned Mesh` component.
