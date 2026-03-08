import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AccountService } from '../../../services/account.service';
import { LoginReq } from '../../../models/login.model';

@Component({
  selector: 'app-login',
  imports: [
    FormsModule, ReactiveFormsModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private _accountService = inject(AccountService);
  private _fB = inject(FormBuilder);

  isLoadingSig = signal<boolean>(false);

  error: string | undefined;
  emailMaxLength: number = 100;
  passwordMinLength: number = 8;
  passwordMaxLength: number = 16;

  loginFg = this._fB.group({
    emailCtrl: ['', [Validators.required, Validators.maxLength(this.emailMaxLength), Validators.pattern(/^([\w\.\-]+)@([\w\-]+)((\.(\w){2,5})+)$/)]],
    passwordCtrl: ['', [Validators.required, Validators.minLength(this.passwordMinLength), Validators.maxLength(this.passwordMaxLength)]]
  })

  get EmailCtrl(): FormControl {
    return this.loginFg.get('emailCtrl') as FormControl;
  }

  get PasswordCtrl(): FormControl {
    return this.loginFg.get('passwordCtrl') as FormControl;
  }

  login(): void {
    this.isLoadingSig.set(true);

    let loginReq: LoginReq = {
      email: this.EmailCtrl.value,
      password: this.PasswordCtrl.value
    }

    this._accountService.login(loginReq).subscribe({
      next: (res) => {
        this.isLoadingSig.set(false);
    },
      error: (err) => { 
        this.isLoadingSig.set(false);
        this.error = err.error;
      }
    })
  }
}
