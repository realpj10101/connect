export interface ChatItem {
    id: string;
    type: string;
    senderUserName: string;
    message: string | null;
    duration: number | null;
    fileSize: number | null;
    createdAt: Date;
    isPlaying: boolean;
    audioUrl?: string;
    audioRef?: HTMLAudioElement;
    isDownloading?: boolean;
    downloadProgress?: number;
    isDownloaded?: boolean;
    imageUrl256?: string; // thumbnail 256
    enlargedUrl?: string; // full image for lightbox
    isDownloadingLarge?: boolean;
}