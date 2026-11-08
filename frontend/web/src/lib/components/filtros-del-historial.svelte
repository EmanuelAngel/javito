<script lang="ts">
	import type { Habito } from '$lib/api/habitos';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';

	// Los filtros del historial (HU-HIS-01): el hábito y el rango de fechas. Acá sólo se lee el control;
	// la pantalla es la dueña de la URL y decide qué hacer con el cambio. El listado de hábitos trae
	// también los archivados, porque su historial sigue siendo consultable (HU-HAB-04).
	export type FiltrosDelHistorial = {
		habitoId: number | null;
		desde: string | null;
		hasta: string | null;
	};

	type Props = FiltrosDelHistorial & {
		habitos: Habito[];
		alCambiar: (filtros: FiltrosDelHistorial) => void;
		alQuitar: () => void;
	};

	let { habitoId, desde, hasta, habitos, alCambiar, alQuitar }: Props = $props();

	function elegirHabito(evento: Event) {
		const valor = (evento.currentTarget as HTMLSelectElement).value;

		alCambiar({ habitoId: valor === '' ? null : Number(valor), desde, hasta });
	}

	function elegirFecha(campo: 'desde' | 'hasta', evento: Event) {
		const valor = (evento.currentTarget as HTMLInputElement).value || null;

		alCambiar({
			habitoId,
			desde: campo === 'desde' ? valor : desde,
			hasta: campo === 'hasta' ? valor : hasta
		});
	}
</script>

<div class="flex flex-col gap-3 sm:flex-row sm:items-end">
	<div class="flex flex-col gap-1.5 sm:max-w-xs sm:flex-1">
		<label class="text-sm font-medium" for="historial-habito">Hábito</label>
		<NativeSelect
			id="historial-habito"
			class="w-full"
			value={habitoId === null ? '' : String(habitoId)}
			onchange={elegirHabito}
		>
			<NativeSelectOption value="">Todos los hábitos</NativeSelectOption>
			{#each habitos as habito (habito.id)}
				<NativeSelectOption value={habito.id}>
					{habito.titulo}{habito.activo ? '' : ' (archivado)'}
				</NativeSelectOption>
			{/each}
		</NativeSelect>
	</div>

	<div class="flex flex-col gap-1.5">
		<label class="text-sm font-medium" for="historial-desde">Desde</label>
		<Input
			id="historial-desde"
			type="date"
			value={desde ?? ''}
			onchange={(evento) => elegirFecha('desde', evento)}
		/>
	</div>

	<div class="flex flex-col gap-1.5">
		<label class="text-sm font-medium" for="historial-hasta">Hasta</label>
		<Input
			id="historial-hasta"
			type="date"
			value={hasta ?? ''}
			onchange={(evento) => elegirFecha('hasta', evento)}
		/>
	</div>

	{#if habitoId !== null || desde !== null || hasta !== null}
		<Button type="button" variant="ghost" onclick={alQuitar}>Quitar filtros</Button>
	{/if}
</div>
