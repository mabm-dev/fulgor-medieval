#ifndef FULGOR_TERRENO_COLOR_INCLUDED
#define FULGOR_TERRENO_COLOR_INCLUDED

// Color del terreno de campaña.
// - Hierba común (FulgorGrass): un solo verde para todo el mapa, en coordenadas del mundo, con zonas húmedas, medias y pajizas que cruzan las
//   casillas. Las piezas de llanura, bosque y colina toman ese tono en sus píxeles de hierba (conservan caminos, tierra y piedras, y parte del
//   relieve de luz de su textura); las laderas de las montañas generadas usan la misma hierba, así que el pie de la montaña casa con la llanura.
// - Montañas generadas: roca en estratos según la altura, hierba en lo poco empinado, más oscuro lo más empinado y cumbres claras.
// El ruido se lee de _FulgorNoise (256×256, cuatro canales de fbm de cuatro octavas con 8 celdas en la base, sin costuras) en vez de calcularse:
// FulgorRuido(q) se parece al fbm de antes en q, con celdas de una unidad, y cuesta una lectura de textura.

half4 FulgorRuido(float2 q) { return SAMPLE_TEXTURE2D(_FulgorNoise, sampler_FulgorNoise, q * 0.125); }

half FulgorLuma(half3 c) { return dot(c, half3(0.3, 0.59, 0.11)); }

half3 FulgorGrass(float3 positionWS)
{
    half macro = FulgorRuido(positionWS.xz * _FulgorMacroScale).r;
    half mid = FulgorRuido(positionWS.xz * (_FulgorMacroScale * 6.0) + 11.3).g;
    half fine = FulgorRuido(positionWS.xz * 9.0 + 3.7).b;
    half3 grass = lerp(_FulgorLushColor.rgb, _FulgorGrassColor.rgb, smoothstep(0.28, 0.5, macro));
    grass = lerp(grass, _FulgorDryColor.rgb, smoothstep(0.54, 0.74, macro) * _FulgorDryStrength);
    return grass * lerp(0.88, 1.08, mid) * lerp(0.93, 1.05, fine);
}

half3 FulgorLandColor(half3 albedo, float3 positionWS)
{
    half3 grass = FulgorGrass(positionWS);
    half pieceLuma = FulgorLuma(albedo);
    // los píxeles de hierba de la pieza (verde muy por encima del azul y no por debajo del rojo) pasan a la hierba común, conservando parte
    // de su relieve de luz; caminos, tierra y piedras se quedan
    half greenness = saturate(((albedo.g - albedo.b) / max(albedo.g, 0.001) - 0.45) * 4.0) * saturate((albedo.g - albedo.r * 0.85) * 20.0);
    half3 shaded = grass * lerp(1.0, pieceLuma / max(_FulgorPieceLuma, 0.02), _FulgorPieceDetail);
    half3 result = lerp(albedo, shaded, greenness * _FulgorUnify);
    return lerp(FulgorLuma(result).xxx, result, _FulgorSaturation);
}

half3 FulgorMountainColor(float3 positionWS, half3 normalWS, half4 vertexColor)
{
    half slope = 1.0 - saturate(normalWS.y);
    half rise = vertexColor.r;     // 0 en el pie de la montaña
    half height01 = vertexColor.g; // altura sobre el borde de la casilla, normalizada
    float2 p = positionWS.xz;

    // roca en estratos (caliza clara y pizarra), rota por el ruido; lo más empinado, más oscuro
    float strata = frac(positionWS.y * _FulgorStrata + FulgorRuido(p * 1.7).a * 1.5);
    half band = smoothstep(0.0, 0.2, strata) * smoothstep(1.0, 0.65, strata);
    half rockNoise = FulgorRuido(p * 5.0 + positionWS.y * 3.0).g;
    half3 rock = lerp(_FulgorRockDark.rgb, _FulgorRockColor.rgb, smoothstep(0.3, 0.7, rockNoise));
    rock *= lerp(0.8, 1.08, band) * lerp(1.0, 0.72, smoothstep(0.45, 0.85, slope));

    // hierba común en lo poco empinado y en el pie
    half3 grass = FulgorGrass(positionWS) * lerp(0.9, 1.05, FulgorRuido(p * 14.0).b);
    grass = lerp(FulgorLuma(grass).xxx, grass, _FulgorSaturation); // la misma saturación que la llanura vecina
    half grassy = smoothstep(0.32, 0.16, slope) * (1.0 - smoothstep(0.35, 0.65, height01));
    grassy = max(grassy, smoothstep(0.18, 0.03, rise));
    grassy *= lerp(0.6, 1.0, FulgorRuido(p * 3.3 + 4.1).r);
    half3 color = lerp(rock, grass, saturate(grassy));

    // cumbres más claras donde la pendiente deja asentarse
    half cap = smoothstep(0.6, 0.78, height01) * smoothstep(0.7, 0.3, slope) * smoothstep(0.35, 0.6, FulgorRuido(p * 2.1 + 9.7).a);
    return lerp(color, _FulgorCapColor.rgb, cap);
}

void FulgorTerrainSurface(inout SurfaceData surfaceData, inout InputData inputData, half4 vertexColor)
{
    if (_FulgorSinRuido > 0.5) // solo para medir el coste del color
    {
#if defined(_FULGOR_MONTANA)
        surfaceData.albedo = lerp(_FulgorRockColor.rgb, _FulgorGrassColor.rgb, saturate(inputData.normalWS.y * 2.0 - 1.0));
#endif
        return;
    }
#if defined(_FULGOR_MONTANA)
    surfaceData.albedo = FulgorMountainColor(inputData.positionWS, inputData.normalWS, vertexColor);
    surfaceData.smoothness = 0.12;
#else
    // la vegetación alta (árboles del bosque) apenas cambia: el retoque es para el suelo
    half ground = 1.0 - smoothstep(0.08, 0.25, inputData.positionWS.y - _FulgorGroundY);
    surfaceData.albedo = lerp(surfaceData.albedo, FulgorLandColor(surfaceData.albedo, inputData.positionWS), ground);
    // cada pieza de llanura es un montículo de pocos centímetros que baja a 0,12 en el borde: la luz dibujaba un panal de hexágonos;
    // en el suelo la luz cae como si fuera plano y se queda el relieve fino del mapa de normales
    inputData.normalWS = normalize(lerp(inputData.normalWS, half3(0, 1, 0), _FulgorFlatten * ground));
#endif
}

#endif
