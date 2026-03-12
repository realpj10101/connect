import { ElementRef, NgZone } from "@angular/core";
import { ChatItem } from "../models/chat-item.model";
import { take } from "rxjs";

export function getStrokeOffset(message: ChatItem, radius = 14): number {
    const circumference = 2 * Math.PI * radius;
    const progress = message.downloadProgress || 0;
    return circumference - (progress / 100) * circumference;
}

export function formatTimer(ms: number): string {
    const sec = Math.floor(ms / 1000) % 60;
    const min = Math.floor(ms / 60000);
    return `${min < 10 ? '0' : ''}${min}:${sec < 10 ? '0' : ''}${sec}`;
}

export function scrollToBottom(container: ElementRef, ngZone: NgZone, smooth = true): void {
    ngZone.onStable.pipe(take(1)).subscribe(() => {
        const el = container?.nativeElement;
        if (!el) return;
        el.scrollTo({
            top: el.scrollHeight,
            behavior: smooth ? 'smooth' : 'auto'
        });
    });
}

export function shouldLoadMore(element: HTMLElement, threshold = 50): boolean {
    return element.scrollTop <= threshold;
}

export function getMessagesClasses(userName: string, loggedUserName: string): string {
    return userName === loggedUserName ? 'system-message' : 'other-user';
}

export function sendMessageIfTextExists(event: SubmitEvent) {
    
}