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

// Suelo de la tierra generada (llanura, bosque, colinas y laderas): la hierba común con lo que la rompe. Todo en coordenadas del mundo, así que
// nada sigue el dibujo de las casillas: manchas de hierba alta, calvas de tierra, flores y piedrecillas sueltas, hojarasca bajo el bosque, arena
// y tierra húmeda en la orilla, tierra en las cuestas y grava al acercarse al pie de una montaña. El ruido fino se apaga solo con la distancia
// (mipmaps), así que de lejos queda el color medio.
half3 FulgorSuelo(float3 positionWS, half slope, half forest, half shore, half scree)
{
    float2 p = positionWS.xz;
    half4 big = FulgorRuido(p * 0.55 + 21.7);
    half4 mid = FulgorRuido(p * 4.3 + 2.9);
    half4 tiny = FulgorRuido(p * 37.0 + 8.2);
    half4 speck = FulgorRuido(p * 83.0 + 4.6);
    half3 grass = FulgorGrass(positionWS);
    // hierba alta, a manchas: más oscura
    half tall = smoothstep(0.50, 0.60, big.g * 0.6 + mid.b * 0.4);
    grass *= lerp(1.0, 0.8, tall) * lerp(0.9, 1.08, tiny.r);
    grass = lerp(FulgorLuma(grass).xxx, grass, _FulgorSaturation);

    // calvas de tierra, más en las cuestas
    half3 earth = _FulgorEarthColor.rgb * lerp(0.8, 1.12, mid.g) * lerp(0.9, 1.07, tiny.g);
    half bare = smoothstep(0.625, 0.665, big.a * 0.6 + mid.r * 0.4 + smoothstep(0.12, 0.4, slope) * 0.12);
    half3 color = lerp(grass, earth, bare * 0.9);

    // flores en la hierba húmeda: motas amarillas, blancas y malvas
    half bloom = smoothstep(0.70, 0.73, speck.b) * smoothstep(0.54, 0.62, big.r) * smoothstep(0.5, 0.56, mid.a) * (1.0 - bare) * (1.0 - forest);
    half3 petal = tiny.a > 0.56 ? half3(0.74, 0.66, 0.2) : (tiny.a > 0.47 ? half3(0.76, 0.76, 0.7) : half3(0.48, 0.3, 0.5));
    color = lerp(color, petal, bloom * 0.45);

    // bosque: hierba en sombra, musgo y hojarasca
    half3 litter = _FulgorLitterColor.rgb * lerp(0.75, 1.2, mid.b) * lerp(0.85, 1.1, tiny.g);
    half3 floorColor = lerp(grass * half3(0.6, 0.7, 0.56), litter, smoothstep(0.54, 0.64, mid.g * 0.6 + tiny.r * 0.4));
    color = lerp(color, floorColor, forest * 0.92);

    // orilla: arena a trozos y, pegada al agua, tierra húmeda más oscura
    half beach = smoothstep(0.42, 0.62, shore + (mid.r - 0.5) * 0.45);
    half3 sand = _FulgorSandColor.rgb * lerp(0.88, 1.08, mid.a) * lerp(0.93, 1.05, tiny.r);
    color = lerp(color, sand * lerp(1.0, 0.7, smoothstep(0.82, 0.97, shore)), beach * 0.9);

    // piedrecillas sueltas, y cada vez más grava hacia el pie de la montaña
    half3 stone = lerp(_FulgorRockDark.rgb, _FulgorRockColor.rgb, smoothstep(0.35, 0.65, tiny.g)) * lerp(0.8, 1.1, tiny.r);
    half pebbles = smoothstep(0.70, 0.73, speck.a * 0.75 + mid.a * 0.25);
    half gravel = smoothstep(0.0, 0.05, (mid.a * 0.2 + tiny.b * 0.4 + speck.g * 0.4) - lerp(0.70, 0.49, scree * scree));
    return lerp(color, stone, max(pebbles * 0.75, gravel * 0.85));
}

// relieve fino del suelo sin más lecturas: la pendiente del ruido en pantalla empuja la normal (Mikkelsen)
half3 FulgorNormalFina(float3 positionWS, half3 normalWS, half amount)
{
    float h = FulgorRuido(positionWS.xz * 6.0 + 5.5).r * 0.7 + FulgorRuido(positionWS.xz * 27.0 + 1.9).g * 0.3;
    float3 dpdx = ddx(positionWS), dpdy = ddy(positionWS);
    float dhdx = ddx(h), dhdy = ddy(h);
    float3 r1 = cross(dpdy, normalWS), r2 = cross(normalWS, dpdx);
    float det = dot(dpdx, r1);
    float3 gradient = sign(det) * (dhdx * r1 + dhdy * r2);
    return normalize(abs(det) * normalWS - (0.035 * amount * _FulgorRelieveFino) * gradient);
}

half3 FulgorMountainColor(float3 positionWS, half3 normalWS, half4 vertexColor, out half rocky)
{
    half slope = 1.0 - saturate(normalWS.y);
    // r: de 0 a 0,5 el pie de la montaña que se acerca (grava), de 0,5 a 1 la subida a la cumbre
    half rise = saturate(vertexColor.r * 2.0 - 1.0);
    half scree = saturate(vertexColor.r * 2.0);
    half height01 = vertexColor.g; // altura sobre el borde de la casilla, normalizada
    float2 p = positionWS.xz;

    // roca en estratos (caliza clara y pizarra), rota por el ruido; lo más empinado, más oscuro
    float strata = frac(positionWS.y * _FulgorStrata + FulgorRuido(p * 1.7).a * 2.6);
    half band = smoothstep(0.0, 0.3, strata) * smoothstep(1.0, 0.6, strata);
    half rockNoise = FulgorRuido(p * 5.0 + positionWS.y * 3.0).g;
    half3 rock = lerp(_FulgorRockDark.rgb, _FulgorRockColor.rgb, smoothstep(0.3, 0.7, rockNoise));
    rock *= lerp(0.9, 1.05, band) * lerp(1.0, 0.72, smoothstep(0.45, 0.85, slope)) * lerp(0.86, 1.1, FulgorRuido(p * 19.0 + 2.2).r);

    // el suelo común en lo poco empinado y en el pie
    half3 grass = FulgorSuelo(positionWS, slope, vertexColor.b, vertexColor.a, scree);
    // el ruido rompe la hierba de las laderas; las colinas (sin roca de montaña) y el pie de la montaña son hierba entera
    half slopeGrass = smoothstep(0.32, 0.16, slope) * (1.0 - smoothstep(0.35, 0.65, height01));
    slopeGrass *= lerp(0.6, 1.0, FulgorRuido(p * 3.3 + 4.1).r);
    half grassy = max(slopeGrass, smoothstep(0.2, 0.02, rise + (FulgorRuido(p * 2.6 + 7.3).b - 0.5) * 0.12));
    half3 color = lerp(rock, grass, saturate(grassy));
    rocky = 1.0 - saturate(grassy);

    // cumbres más claras donde la pendiente deja asentarse
    half cap = smoothstep(0.6, 0.78, height01) * smoothstep(0.7, 0.3, slope) * smoothstep(0.35, 0.6, FulgorRuido(p * 2.1 + 9.7).a);
    return lerp(color, _FulgorCapColor.rgb, cap);
}

// --- Edificios de casilla: suelo pisado alrededor y base fundida con él ---
// _FulgorHuellas (CampaignBuildingGroundMap) guarda, para todo el tablero, la distancia al borde de la huella del edificio más cercano: de −0,1
// a 0,4 unidades en 0–1 (en los edificios pequeños, como el pozo, la distancia va multiplicada: su franja es más estrecha). El terreno pinta
// tierra pisada a trozos alrededor de la huella, con la sombra de contacto junto a la pared; el edificio (modo _FULGOR_EDIFICIO) pinta su base
// con la misma tierra y el mismo ruido en el mismo sitio, así no hay junta.

half FulgorDistanciaHuella(float3 positionWS)
{
    float2 uv = positionWS.xz * _FulgorHuellasST.xy + _FulgorHuellasST.zw;
    return SAMPLE_TEXTURE2D(_FulgorHuellas, sampler_FulgorHuellas, uv).r * 0.5 - 0.1;
}

// grano común: manchas de unos 25 cm (coarse) y de unos 4 cm (fine)
void FulgorGranoPisado(float2 p, out half4 coarse, out half4 fine)
{
    coarse = FulgorRuido(p * 4.0 + 7.7);
    fine = FulgorRuido(p * 23.0 + 1.3);
}

half3 FulgorTierraPisada(half3 earth, half4 coarse, half4 fine)
{
    return earth * lerp(0.82, 1.12, coarse.g) * lerp(0.9, 1.06, fine.r);
}

// qué trozos quedan pisados: ruido en dos escalas, repartido casi de 0 a 1; un punto queda pisado si su valor pasa la distancia relativa
half FulgorTrozosPisados(half4 coarse, half4 fine)
{
    return saturate((fine.g * 0.6 + coarse.b * 0.4 - 0.27) * 2.2);
}

half3 FulgorMatasColor(float3 positionWS)
{
    half3 grass = FulgorGrass(positionWS) * _FulgorMatasLuz;
    return lerp(FulgorLuma(grass).xxx, grass, _FulgorSaturation);
}

// tierra pisada alrededor de la huella, a trozos de borde nítido: junto a la pared casi toda pisada y hacia fuera cada vez menos, con hierba
// seca entre la hierba y la tierra, piedrecillas, y alguna mata pegada a la pared (ahí no se pisa)
half3 FulgorSueloPisado(half3 albedo, float3 positionWS, half distance, half3 earthColor, half tufts)
{
    half4 coarse, fine;
    FulgorGranoPisado(positionWS.xz, coarse, fine);
    half4 grain = FulgorRuido(positionWS.xz * 57.0 + 5.1); // unos 2 cm
    half reach = distance / (_FulgorPisadoAncho * lerp(0.7, 1.3, coarse.r));
    half patches = FulgorTrozosPisados(coarse, fine);
    half worn = smoothstep(reach - 0.06, reach + 0.06, patches);
    half dry = smoothstep(reach - 0.32, reach - 0.12, patches) * (1.0 - smoothstep(1.0, 1.35, reach));

    half3 earth = FulgorTierraPisada(earthColor, coarse, fine);
    earth *= lerp(1.0, 1.2, smoothstep(0.66, 0.72, grain.b)) * lerp(1.0, 0.8, smoothstep(0.68, 0.74, grain.a));
    half3 straw = lerp(albedo, _FulgorDryColor.rgb * FulgorLuma(albedo) / max(FulgorLuma(_FulgorDryColor.rgb), 0.05), 0.55);
    half3 color = lerp(albedo, straw, dry * _FulgorPisadoFuerza);
    color = lerp(color, earth, worn * _FulgorPisadoFuerza);
    half wild = smoothstep(0.58, 0.75, fine.b) * (1.0 - smoothstep(0.0, 0.035, distance)) * tufts;
    color = lerp(color, FulgorMatasColor(positionWS), wild);
    half contact = 1.0 - smoothstep(0.0, 0.05, distance);
    return color * lerp(1.0, 0.8, contact * contact);
}

// base del edificio: hasta unos centímetros sobre su base (_FulgorBaseY) toma la tierra pisada de su casilla, a trozos con el mismo ruido que
// el suelo (en la pared, salpicaduras de alto desigual), alguna mata que trepa y el primer centímetro más oscuro, como el suelo junto a la pared
void FulgorBuildingBase(inout SurfaceData surfaceData, inout InputData inputData, float3 positionWS, half upward)
{
    float h = positionWS.y - _FulgorBaseY;
    bool low = h <= _FulgorBaseAlto * 1.4 + 0.01;
    // el borde de la losa (lo que mira hacia arriba a pocos centímetros del suelo, junto al contorno de la huella)
    bool rim = _FulgorHuellasActivas > 0.5 && h < 0.11 && upward > 0.55;
    if (!low && !rim) return;
    half4 coarse, fine;
    FulgorGranoPisado(positionWS.xz, coarse, fine);
    half rise = h / (_FulgorBaseAlto * lerp(0.6, 1.4, coarse.r));
    half climb = smoothstep(0.58, 0.78, fine.b) * (1.0 - smoothstep(0.0, 0.7, rise)) * _FulgorMatas;
    half3 ground = lerp(FulgorTierraPisada(_FulgorPisadoColor.rgb, coarse, fine), FulgorMatasColor(positionWS), climb);
    half dirt = low ? smoothstep(rise - 0.06, rise + 0.06, FulgorTrozosPisados(coarse, fine) * 0.7 + 0.3) : 0.0;
    if (rim)
    {
        // en sus últimos centímetros la tierra y las matas de la casilla se comen la losa a trozos: deja de verse su contorno recto
        half edge = smoothstep(-0.085, -0.005, FulgorDistanciaHuella(positionWS));
        half3 edgeGround = lerp(ground, FulgorMatasColor(positionWS), smoothstep(0.55, 0.7, fine.b) * _FulgorMatas);
        half eaten = smoothstep(1.0 - edge * 1.25, 1.06 - edge * 1.25, fine.g * 0.55 + coarse.b * 0.45 + 0.24);
        surfaceData.albedo = lerp(surfaceData.albedo, edgeGround, eaten * _FulgorPisadoFuerza);
        dirt = max(dirt, eaten);
        // y con el color se va el relieve de las losas: donde manda la tierra, la luz cae como en el suelo
        inputData.normalWS = normalize(lerp(inputData.normalWS, half3(0, 1, 0), eaten * 0.85));
    }
    if (low) surfaceData.albedo = lerp(surfaceData.albedo, ground, dirt * _FulgorPisadoFuerza) * lerp(0.8, 1.0, smoothstep(0.0, 0.012, h));
    surfaceData.smoothness = lerp(surfaceData.smoothness, 0.06, dirt);
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
#if defined(_FULGOR_ARBOL)
    // árboles: el color de vértice trae la sombra propia de la copa (más oscura por dentro y por abajo) y el tono de cada ejemplar; el ruido
    // del mundo cambia un poco el verde de un rodal a otro
    surfaceData.albedo *= vertexColor.rgb * lerp(0.84, 1.14, FulgorRuido(inputData.positionWS.xz * 0.6 + 3.1).g);
    surfaceData.smoothness = 0.04;
#elif defined(_FULGOR_EDIFICIO)
    FulgorBuildingBase(surfaceData, inputData, inputData.positionWS, inputData.normalWS.y);
#else
    // Tinte de la casilla: la niebla (explorada, ×0,42), la selección o el paso del ratón llegan en _BaseColor con un MaterialPropertyBlock. El
    // color del terreno se calcula con el color propio del material (_FulgorBaseTint) y el tinte se aplica al final, también a la hierba y a la
    // roca que este sombreador pinta por su cuenta: sin esto, una casilla explorada dejaba la hierba a plena luz.
    half3 tint = _BaseColor.rgb / max(_FulgorBaseTint.rgb, 0.001);
    surfaceData.albedo /= max(tint, 0.001);
    #if defined(_FULGOR_MONTANA)
    half rock;
    surfaceData.albedo = FulgorMountainColor(inputData.positionWS, inputData.normalWS, vertexColor, rock);
    surfaceData.smoothness = 0.08;
    inputData.normalWS = FulgorNormalFina(inputData.positionWS, inputData.normalWS, lerp(1.0, 2.2, rock));
    // junto a un edificio en la montaña, grava en vez de tierra, y sin hierba; bajo el bosque, tierra oscura
    half ground = 1.0;
    half3 earth = lerp(lerp(_FulgorPisadoColor.rgb, _FulgorLitterColor.rgb * 1.1, vertexColor.b), _FulgorRockColor.rgb * 0.88, rock);
    half tufts = _FulgorMatas * (1.0 - rock);
    #else
    // la vegetación alta (árboles del bosque) apenas cambia: el retoque es para el suelo
    half ground = 1.0 - smoothstep(0.08, 0.25, inputData.positionWS.y - _FulgorGroundY);
    surfaceData.albedo = lerp(surfaceData.albedo, FulgorLandColor(surfaceData.albedo, inputData.positionWS), ground);
    // cada pieza de llanura es un montículo de pocos centímetros que baja a 0,12 en el borde: la luz dibujaba un panal de hexágonos;
    // en el suelo la luz cae como si fuera plano y se queda el relieve fino del mapa de normales
    inputData.normalWS = normalize(lerp(inputData.normalWS, half3(0, 1, 0), _FulgorFlatten * ground));
    half3 earth = _FulgorPisadoColor.rgb;
    half tufts = _FulgorMatas;
    #endif
    if (_FulgorHuellasActivas > 0.5)
    {
        half distance = FulgorDistanciaHuella(inputData.positionWS);
        if (distance < 0.38)
            surfaceData.albedo = lerp(surfaceData.albedo, FulgorSueloPisado(surfaceData.albedo, inputData.positionWS, distance, earth, tufts), ground);
    }
    surfaceData.albedo *= tint;
#endif
}

#endif
