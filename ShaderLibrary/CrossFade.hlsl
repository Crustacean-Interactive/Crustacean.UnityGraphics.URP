#ifndef UNIVERSAL_CROSSFADE_HLSL
#define UNIVERSAL_CROSSFADE_HLSL

// zCubed: Unity flips the sign when blending in and out, WHY!?!?!?

#if defined(LOD_FADE_CROSSFADE)
#define CROSSFADE_LOD(positionCS) \
    float xfadeNoise = InterleavedGradientNoise(positionCS.xy, 0); \
    \
    float xfadePositive = sign(unity_LODFade.x) == 1; \
    float xfadeBelow = unity_LODFade.x < xfadeNoise; \
    float xfadeAbove = abs(unity_LODFade.x) > xfadeNoise; \
    \
    float xfadeCullFac = lerp(xfadeAbove, xfadeBelow, xfadePositive) - 0.5; \
    clip(-xfadeCullFac);
#else
#define CROSSFADE_LOD(positionCS)
#endif

#endif
