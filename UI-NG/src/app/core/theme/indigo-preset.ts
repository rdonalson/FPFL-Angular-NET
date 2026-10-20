import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * "Polished Indigo": Aura with an indigo primary instead of Aura's default emerald.
 * Every PrimeNG component (buttons, inputs, tabs, checkboxes, focus rings) picks this up.
 */
export const IndigoPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{indigo.50}',
      100: '{indigo.100}',
      200: '{indigo.200}',
      300: '{indigo.300}',
      400: '{indigo.400}',
      500: '{indigo.500}',
      600: '{indigo.600}',
      700: '{indigo.700}',
      800: '{indigo.800}',
      900: '{indigo.900}',
      950: '{indigo.950}',
    },
  },
});
