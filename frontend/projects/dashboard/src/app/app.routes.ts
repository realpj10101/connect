import { Routes } from '@angular/router';
import { LoginComponent } from './components/account/login/login.component';
import { RegisterComponent } from './components/account/register/register.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard } from './guards/auth.guard';
import { ProfileComponent } from './components/profile/profile.component';
import { CreateRoomComponent } from './components/rooms/create-room/create-room.component';
import { authLoggedInGuard } from './guards/auth-logged-in.guard';
import { RoomViewComponent } from './components/rooms/room-view/room-view.component';
import { RoomDetailsComponent } from './components/rooms/room-details/room-details.component';

export const routes: Routes = [
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [authGuard],
        canActivateChild: [],
        children: [
            { path: '', component: DashboardComponent },
            { path: 'dashboard', component: DashboardComponent },
            { path: 'profile', component: ProfileComponent },
            { path: 'create-room', component: CreateRoomComponent },
            { path: 'room-view/:id', component: RoomViewComponent},
            { path: 'room-details/:id', component: RoomDetailsComponent},
            { path: 'create-room', component: CreateRoomComponent},
            { path:'profile', component: ProfileComponent}
        ]
    },
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [authLoggedInGuard],
        canActivateChild: [],
        children: [
            { path: 'sign-in', component: LoginComponent },
            { path: 'sign-up', component: RegisterComponent },
        ]
    },
];
