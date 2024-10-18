#ifndef SHADERLIB_STRAYED_UTILS_HLSL
#define SHADERLIB_STRAYED_UTILS_HLSL

// Old Unity function that doesn't exist in SRPs for some reason
half LerpOneTo(half b, half t)
{
    half oneMinusT = 1 - t;
    return oneMinusT + b * t;
}

#endif //SHADERLIB_STRAYED_UTILS_HLSL
