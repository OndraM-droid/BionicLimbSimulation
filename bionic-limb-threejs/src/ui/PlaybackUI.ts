import type { PlaybackController, PlaybackState } from '../playback/PlaybackController.js';
import type { GestureResult } from '../classification/IGestureClassifier.js';

export class PlaybackUI {
  private _fileInput: HTMLInputElement;

  constructor(controller: PlaybackController) {
    const gestureLabel = document.getElementById('gesture-label')!;
    const classifierModeLabel = document.getElementById('classifier-mode-label')!;
    const progressBar = document.getElementById('progress-bar') as HTMLInputElement;
    const btnPlayPause = document.getElementById('btn-play-pause')!;
    const btnNextStep = document.getElementById('btn-next-step')!;
    const btnPrevStep = document.getElementById('btn-prev-step')!;
    const btnImport = document.getElementById('btn-import')!;
    const errorLabel = document.getElementById('error-label')!;

    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = '.csv';
    fileInput.style.display = 'none';
    document.body.appendChild(fileInput);
    this._fileInput = fileInput;

    btnPlayPause.addEventListener('click', () => {
      if (controller.state === 'Playing') controller.pause();
      else controller.play();
    });

    btnNextStep.addEventListener('click', () => controller.nextStep());
    btnPrevStep.addEventListener('click', () => controller.previousStep());
    btnImport.addEventListener('click', () => fileInput.click());

    fileInput.addEventListener('change', () => {
      const file = fileInput.files?.[0];
      if (!file) return;
      const reader = new FileReader();
      reader.onload = (e) => {
        const text = e.target?.result as string;
        try {
          controller.loadFile(text);
          errorLabel.style.display = 'none';
        } catch (err) {
          errorLabel.textContent = err instanceof Error ? err.message : String(err);
          errorLabel.style.display = 'block';
        }
      };
      reader.readAsText(file);
    });

    controller.addEventListener('gestureChanged', (e: Event) => {
      const result = (e as CustomEvent<{ result: GestureResult }>).detail.result;
      gestureLabel.textContent = `${result.gestureName} (${(result.confidence * 100).toFixed(0)}%)`;
    });

    controller.addEventListener('progressChanged', (e: Event) => {
      const progress = (e as CustomEvent<{ progress: number }>).detail.progress;
      progressBar.value = String(progress * 100);
    });

    controller.addEventListener('stateChanged', (e: Event) => {
      const state = (e as CustomEvent<{ state: PlaybackState }>).detail.state;
      btnPlayPause.textContent = state === 'Playing' ? '\u23F8' : '\u25B6';
      classifierModeLabel.textContent = controller.classifierMode;
    });
  }

  dispose(): void {
    document.body.removeChild(this._fileInput);
  }
}
