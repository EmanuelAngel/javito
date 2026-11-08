<script lang="ts">
	import type { Rutina } from '$lib/api/rutinas';
	import { NativeSelect, NativeSelectOption } from '$lib/components/ui/native-select/index.js';

	// El selector de rutina de la pantalla del día (HU-RUT-04). Sólo lee el control: la pantalla es la
	// dueña de la URL. Llegan sólo las rutinas activas; `null` es «todos».
	type Props = {
		rutinas: Rutina[];
		rutinaId: number | null;
		alElegir: (rutinaId: number | null) => void;
	};

	let { rutinas, rutinaId, alElegir }: Props = $props();

	function elegir(evento: Event) {
		const valor = (evento.currentTarget as HTMLSelectElement).value;

		alElegir(valor === '' ? null : Number(valor));
	}
</script>

<div class="flex flex-col gap-1.5 sm:max-w-xs">
	<label class="text-sm font-medium" for="hoy-rutina">Rutina</label>
	<NativeSelect
		id="hoy-rutina"
		class="w-full"
		value={rutinaId === null ? '' : String(rutinaId)}
		onchange={elegir}
	>
		<NativeSelectOption value="">Todos los hábitos</NativeSelectOption>
		{#each rutinas as rutina (rutina.id)}
			<NativeSelectOption value={rutina.id}>{rutina.nombre}</NativeSelectOption>
		{/each}
	</NativeSelect>
</div>
