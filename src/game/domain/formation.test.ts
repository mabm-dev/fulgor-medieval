import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearFormacion,
  type OpcionesFormacion,
  type TipoFormacion,
} from './formation'

/**
 * Perfil ilustrativo, no el catálogo definitivo: los números de
 * `content/formations.ts` se deciden en su propio paso.
 */
function crearOpciones(
  cambios: Partial<OpcionesFormacion> = {},
): OpcionesFormacion {
  return {
    id: 'lanceros-1',
    nombre: 'Lanceros concejiles',
    tipo: 'infanteria',
    cantidad: 40,
    saludPorIntegrante: 10,
    ataque: 6,
    defensa: 5,
    danoMin: 2,
    danoMax: 5,
    movimiento: 3,
    iniciativa: 4,
    alcance: 1,
    disciplina: 55,
    ...cambios,
  }
}

describe('formación', () => {
  it('crea una formación válida e inmutable', () => {
    const formacion = crearFormacion(
      crearOpciones(),
    )

    expect(formacion).toEqual({
      id: 'lanceros-1',
      nombre: 'Lanceros concejiles',
      tipo: 'infanteria',
      cantidad: 40,
      saludPorIntegrante: 10,
      ataque: 6,
      defensa: 5,
      danoMin: 2,
      danoMax: 5,
      movimiento: 3,
      iniciativa: 4,
      alcance: 1,
      disciplina: 55,
      rasgos: [],
      fatiga: 0,
      moral: 100,
    })
    expect(
      Object.isFrozen(formacion),
    ).toBe(true)
    expect(
      Object.isFrozen(formacion.rasgos),
    ).toBe(true)
  })

  it('normaliza sus textos', () => {
    const formacion = crearFormacion(
      crearOpciones({
        id: '  lanceros-1  ',
        nombre: '  Lanceros concejiles  ',
      }),
    )

    expect(formacion.id).toBe(
      'lanceros-1',
    )
    expect(formacion.nombre).toBe(
      'Lanceros concejiles',
    )
  })

  it('acepta rasgos y respeta la fatiga y moral explícitas', () => {
    const formacion = crearFormacion(
      crearOpciones({
        rasgos: [
          'leva_concejil',
          '  disciplinada  ',
        ],
        fatiga: 30,
        moral: 70,
      }),
    )

    expect(formacion.rasgos).toEqual([
      'leva_concejil',
      'disciplinada',
    ])
    expect(formacion.fatiga).toBe(30)
    expect(formacion.moral).toBe(70)
  })

  it('rechaza un identificador vacío', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ id: '   ' }),
      ),
    ).toThrow(
      'El identificador es obligatorio',
    )
  })

  it('rechaza un nombre vacío', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ nombre: '' }),
      ),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })

  it('rechaza un tipo desconocido', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({
          tipo:
            'artilleria' as TipoFormacion,
        }),
      ),
    ).toThrow(
      'El tipo de formación no es válido',
    )
  })

  it('rechaza una cantidad menor que uno', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ cantidad: 0 }),
      ),
    ).toThrow(
      'La cantidad debe ser un entero positivo',
    )
  })

  it('rechaza una cantidad decimal', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ cantidad: 3.5 }),
      ),
    ).toThrow(
      'La cantidad debe ser un entero positivo',
    )
  })

  it('rechaza salud por integrante no positiva', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({
          saludPorIntegrante: 0,
        }),
      ),
    ).toThrow(
      'La salud por integrante debe ser un entero positivo',
    )
  })

  it('rechaza un ataque no positivo', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ ataque: 0 }),
      ),
    ).toThrow(
      'El ataque debe ser un entero positivo',
    )
  })

  it('permite una defensa igual a cero', () => {
    const formacion = crearFormacion(
      crearOpciones({ defensa: 0 }),
    )

    expect(formacion.defensa).toBe(0)
  })

  it('rechaza una defensa negativa', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ defensa: -1 }),
      ),
    ).toThrow(
      'La defensa debe ser un entero no negativo',
    )
  })

  it('rechaza un daño máximo menor que el mínimo', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({
          danoMin: 5,
          danoMax: 2,
        }),
      ),
    ).toThrow(
      'El daño máximo no puede ser menor que el mínimo',
    )
  })

  it('acepta un daño mínimo igual al máximo', () => {
    const formacion = crearFormacion(
      crearOpciones({
        danoMin: 3,
        danoMax: 3,
      }),
    )

    expect(formacion.danoMin).toBe(3)
    expect(formacion.danoMax).toBe(3)
  })

  it('rechaza un movimiento no positivo', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ movimiento: 0 }),
      ),
    ).toThrow(
      'El movimiento debe ser un entero positivo',
    )
  })

  it('rechaza una iniciativa no positiva', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ iniciativa: 0 }),
      ),
    ).toThrow(
      'La iniciativa debe ser un entero positivo',
    )
  })

  it('rechaza un alcance no positivo', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ alcance: 0 }),
      ),
    ).toThrow(
      'El alcance debe ser un entero positivo',
    )
  })

  it('rechaza una disciplina negativa', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ disciplina: -1 }),
      ),
    ).toThrow(
      'La disciplina debe ser un entero entre 0 y 100',
    )
  })

  it('rechaza una disciplina mayor que cien', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ disciplina: 101 }),
      ),
    ).toThrow(
      'La disciplina debe ser un entero entre 0 y 100',
    )
  })

  it('rechaza una fatiga fuera de rango', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ fatiga: 101 }),
      ),
    ).toThrow(
      'La fatiga debe ser un entero entre 0 y 100',
    )
  })

  it('rechaza una moral fuera de rango', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ moral: -1 }),
      ),
    ).toThrow(
      'La moral debe ser un entero entre 0 y 100',
    )
  })

  it('rechaza un rasgo vacío', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({ rasgos: ['  '] }),
      ),
    ).toThrow(
      'Un rasgo no puede estar vacío',
    )
  })

  it('rechaza rasgos duplicados', () => {
    expect(() =>
      crearFormacion(
        crearOpciones({
          rasgos: [
            'leva_concejil',
            'leva_concejil',
          ],
        }),
      ),
    ).toThrow(
      'Rasgo repetido: leva_concejil',
    )
  })
})
