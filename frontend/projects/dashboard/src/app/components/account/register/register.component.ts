import { Component, inject, signal } from '@angular/core';
import { AccountService } from '../../../services/account.service';
import { FormBuilder, FormControl, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RegiserReq } from '../../../models/register.model';

@Component({
  selector: 'app-register',
  imports: [
    FormsModule, ReactiveFormsModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private _accountService = inject(AccountService);
  private _fB = inject(FormBuilder);

  isLoadingSig = signal<boolean>(false);

  hide: boolean = true;
  emailMaxLength: number = 100;
  userNameMinLength: number = 1;
  userNameMaxLength: number = 50;
  passwordMinLength: number = 8;
  passwordMaxLength: number = 16;
  passwordsNotMatch = false;
  error: string | undefined;

  togglePassword(): void {
    this.hide = !this.hide;
  }

  registerFg = this._fB.group({
    emailCtrl: ['', [Validators.required, Validators.maxLength(this.emailMaxLength), Validators.pattern(/^([\w\.\-]+)@([\w\-]+)((\.(\w){2,5})+)$/)]],
    userNameCtrl: ['', [Validators.required, Validators.minLength(this.userNameMinLength), Validators.maxLength(this.userNameMaxLength)]],
    passwordCtrl: ['', [Validators.required, Validators.minLength(this.passwordMinLength), Validators.maxLength(this.passwordMaxLength)]],
    confirmPasswordCtrl: ['', [Validators.required]]
  })

  get EmailCtrl(): FormControl {
    return this.registerFg.get('emailCtrl') as FormControl;
  }

  get UserNameCtrl(): FormControl {
    return this.registerFg.get('userNameCtrl') as FormControl;
  }

  get PasswordCtrl(): FormControl {
    return this.registerFg.get('passwordCtrl') as FormControl;
  }

  get ConfirmPasswordCtrl(): FormControl {
    return this.registerFg.get('confirmPasswordCtrl') as FormControl;
  }

  register(): void {
    this.isLoadingSig.set(true);

    if (this.PasswordCtrl.value === this.ConfirmPasswordCtrl.value) {
      this.passwordsNotMatch = false;

      let registerReq: RegiserReq = {
        email: this.EmailCtrl.value,
        userName: this.UserNameCtrl.value,
        password: this.PasswordCtrl.value,
        confirmPassword: this.ConfirmPasswordCtrl.value
      }

      this._accountService.regiser(registerReq).subscribe({
        next: (res) => {
          this.isLoadingSig.set(false);
        },
        error: (err) => {
          this.isLoadingSig.set(false),
          this.error = err.error
        }
      })
    }
    else {
      this.passwordsNotMatch = true;
      this.isLoadingSig.set(false);
    }
  }
}
