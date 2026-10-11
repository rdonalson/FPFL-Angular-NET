import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Group validator: the two named controls must hold the same value. */
export function passwordsMatch(passwordKey: string, confirmKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey)?.value;
    const confirm = group.get(confirmKey)?.value;
    return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
  };
}
