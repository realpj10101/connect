import { Routes } from '@angular/router';
import { LoginComponent } from '../../../dashboard/src/app/components/account/login/login.component';
import { HomeComponent } from './components/home/home.component';

export const routes: Routes = [
    { path: '', component: HomeComponent }
];
