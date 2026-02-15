import { AIContent } from "../models/ClientInteractionResult";

/**
 * Utility class for converting various inputs to AIContent format.
 * Provides helpers for files, camera, geolocation, etc.
 */
export class AIContentConverter {
    /**
     * Converts a File/Blob to Base64 DataContent.
     * @param file - File or Blob to convert.
     * @returns AIContent with Base64 data.
     * 
     * @example
     * const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
     * const file = fileInput.files[0];
     * const content = await AIContentConverter.fromFile(file);
     */
    public static async fromFile(file: File | Blob): Promise<AIContent> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = () => {
                const result = reader.result as string;
                // Extract Base64 part after "data:...;base64,"
                const base64 = result.split(",")[1];

                resolve({
                    type: "data",
                    data: base64,
                    mediaType: file.type || "application/octet-stream"
                });
            };

            reader.onerror = () => {
                reject(new Error("Failed to read file"));
            };

            reader.readAsDataURL(file);
        });
    }

    /**
     * Converts plain text to TextContent.
     * @param text - Text string.
     * @returns AIContent with text.
     * 
     * @example
     * const content = AIContentConverter.fromText("Hello world");
     */
    public static fromText(text: string): AIContent {
        return {
            type: "text",
            text
        };
    }

    /**
     * Captures camera frame and returns Base64 DataContent.
     * @param constraints - MediaStreamConstraints for camera (default: { video: true }).
     * @param width - Canvas width (default: 640).
     * @param height - Canvas height (default: 480).
     * @returns AIContent with Base64 JPEG image.
     * 
     * @example
     * const content = await AIContentConverter.fromCamera();
     */
    public static async fromCamera(
        constraints: MediaStreamConstraints = { video: true },
        width: number = 640,
        height: number = 480
    ): Promise<AIContent> {
        const stream = await navigator.mediaDevices.getUserMedia(constraints);

        try {
            const video = document.createElement("video");
            video.srcObject = stream;
            video.play();

            // Wait for video to be ready
            await new Promise((resolve) => {
                video.onloadedmetadata = resolve;
            });

            // Capture frame
            const canvas = document.createElement("canvas");
            canvas.width = width;
            canvas.height = height;
            const ctx = canvas.getContext("2d")!;
            ctx.drawImage(video, 0, 0, width, height);

            // Convert to Base64
            const dataUrl = canvas.toDataURL("image/jpeg", 0.9);
            const base64 = dataUrl.split(",")[1];

            return {
                type: "data",
                data: base64,
                mediaType: "image/jpeg"
            };
        } finally {
            // Stop camera
            stream.getTracks().forEach((track) => track.stop());
        }
    }

    /**
     * Gets user's geolocation as TextContent (JSON format).
     * @param options - GeolocationOptions.
     * @returns AIContent with JSON coordinates.
     * 
     * @example
     * const content = await AIContentConverter.fromGeolocation();
     * // Result: { type: "text", text: '{"latitude":45.1234,"longitude":7.5678}' }
     */
    public static async fromGeolocation(
        options?: PositionOptions
    ): Promise<AIContent> {
        return new Promise((resolve, reject) => {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const coordinates = {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        altitude: position.coords.altitude,
                        altitudeAccuracy: position.coords.altitudeAccuracy,
                        heading: position.coords.heading,
                        speed: position.coords.speed
                    };

                    resolve({
                        type: "text",
                        text: JSON.stringify(coordinates)
                    });
                },
                (error) => {
                    reject(new Error(`Geolocation error: ${error.message}`));
                },
                options
            );
        });
    }

    /**
     * Converts multiple files to DataContent array.
     * @param files - FileList or File array.
     * @returns Array of AIContent.
     * 
     * @example
     * const files = fileInput.files;
     * const contents = await AIContentConverter.fromMultipleFiles(files);
     */
    public static async fromMultipleFiles(
        files: FileList | File[]
    ): Promise<AIContent[]> {
        const fileArray = Array.from(files);
        return Promise.all(fileArray.map((file) => this.fromFile(file)));
    }

    /**
     * Records audio from microphone and returns Base64 DataContent.
     * @param durationMs - Recording duration in milliseconds (default: 5000).
     * @param mimeType - Audio MIME type (default: "audio/webm").
     * @returns AIContent with Base64 audio.
     * 
     * @example
     * const content = await AIContentConverter.fromMicrophone(3000);
     */
    public static async fromMicrophone(
        durationMs: number = 5000,
        mimeType: string = "audio/webm"
    ): Promise<AIContent> {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

        try {
            const mediaRecorder = new MediaRecorder(stream, { mimeType });
            const chunks: Blob[] = [];

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    chunks.push(event.data);
                }
            };

            mediaRecorder.start();

            // Wait for duration
            await new Promise((resolve) => setTimeout(resolve, durationMs));

            // Set onstop handler BEFORE calling stop to avoid race condition
            await new Promise<void>((resolve) => {
                mediaRecorder.onstop = () => resolve();
                mediaRecorder.stop();
            });

            const audioBlob = new Blob(chunks, { type: mimeType });
            return this.fromFile(audioBlob);
        } finally {
            stream.getTracks().forEach((track) => track.stop());
        }
    }
}
