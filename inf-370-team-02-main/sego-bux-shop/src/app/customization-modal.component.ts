import {
  Component, AfterViewInit, ViewChild, ElementRef, Input, Output, EventEmitter, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import Konva from 'konva';
import 'konva/lib/shapes/Transformer';

import { TemplateDto, TemplateService } from './services/template.service';
import { CartItem } from './services/cart.service';
import { environment } from '../environments/environment';
import { ToastService } from './shared/toast.service';

@Component({
  selector:   'app-customization-modal',
  standalone: true,
  imports:    [CommonModule, FormsModule],
  templateUrl: './customization-modal.component.html',
  styleUrls:   ['./customization-modal.component.scss']
})
export class CustomizationModalComponent implements AfterViewInit, OnDestroy {
  @Input()  product!: CartItem;
  @Output() save   = new EventEmitter<any>();
  @Output() close  = new EventEmitter<void>();

  @ViewChild('canvasContainer', { static: false })
  container!: ElementRef<HTMLDivElement>;

  templates: TemplateDto[] = [];
  customization = {
    template: '',
    customText: '',
    font: 'Arial',
    fontSize: 16,
    color: '#000000',
    uploadedImagePath: '',
    textPosition: { x: 20, y: 20 },
    imagePosition: { x: 50, y: 50 },
    imageWidth: 100,
    imageHeight: 100,
    snapshot: '',
    uploadedImageFile: undefined as File | undefined,
    snapshotFile: undefined as File | undefined
  };
    appliedColor: string = '#000000'; // 👈 NEW for showing applied color


  private stage!: Konva.Stage;
  private layer!: Konva.Layer;
  private templateNode?: Konva.Image;
  public textNode!: Konva.Text;
  public uploadedImageNode?: Konva.Image;
  private imageTransformer?: Konva.Transformer;
  public imageSelected = false;
  private isDestroyed = false;

  constructor(
    private tplSvc: TemplateService,
    private toastSvc: ToastService
  ) {}

  ngAfterViewInit(): void {
    if (!this.container?.nativeElement) return;
    this.stage = new Konva.Stage({
      container: this.container.nativeElement,
      width:  300,
      height: 300
    });
    this.layer = new Konva.Layer();
    this.stage.add(this.layer);

    this.textNode = new Konva.Text({
      text:       this.customization.customText,
      x:          this.customization.textPosition.x,
      y:          this.customization.textPosition.y,
      fontSize:   this.customization.fontSize,
      fontFamily: this.customization.font,
      fill:       this.customization.color,
      draggable:  true
    });
    this.textNode.on('dragend', () =>
      this.customization.textPosition = this.textNode.position()
    );
    this.layer.add(this.textNode);

    // --- Fallback to product image if no template is available ---
    this.tplSvc.getByProduct(this.product.id).subscribe(list => {
      if (list && list.length > 0) {
        this.templates = list;
        this.customization.template = list[0].filePath;
        this.loadBackground(list[0].filePath);
      } else {
        // No template, fallback to product image
        const fallback = this.product.imageUrl || '';
        this.templates = [{
          templateID: 0,
          name:       this.product.name,
          filePath:   fallback
        }];
        this.customization.template = fallback;
        this.loadBackground(fallback);
      }
    });

    this.stage.on('click', (e: any) => {
      if (!e.target || e.target === this.stage) {
        this.imageSelected = false;
        this.hideImageTransformer();
      }
    });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    if (this.stage) {
      this.stage.destroy();
    }
  }

  onTemplateChange(path: string): void {
    this.customization.template = path;
    this.loadBackground(path);
  }

  private loadBackground(path: string) {
    if (!path) return;
    const baseUrl = environment.apiUrl.replace('/api', '');
    // Accepts http(s), data URL, or backend-relative path
    const url = path.startsWith('http') || path.startsWith('data:')
      ? path
      : `${baseUrl}${path}`;

    if (this.stage && this.templateNode) {
      this.templateNode.destroy();
      this.templateNode = undefined;
    }

    const img = new window.Image();
    img.crossOrigin = 'anonymous';
    img.src = url;
    img.onload = () => {
      if (this.isDestroyed) return;
      
      this.templateNode = new Konva.Image({
        image:     img,
        x:         0,
        y:         0,
        width:     300,
        height:    300,
        draggable: false
      });
      this.layer.add(this.templateNode);
      this.templateNode.moveToBottom();
      this.layer.batchDraw();
    };
    img.onerror = () => {
      if (this.isDestroyed) return;
      
      // If image fails to load, fill with neutral gray (never blank)
      const fallbackCanvas = document.createElement('canvas');
      fallbackCanvas.width = 300;
      fallbackCanvas.height = 300;
      const ctx = fallbackCanvas.getContext('2d')!;
      ctx.fillStyle = '#f5f5f5';
      ctx.fillRect(0, 0, 300, 300);

      this.templateNode = new Konva.Image({
        image: fallbackCanvas,
        x: 0,
        y: 0,
        width: 300,
        height: 300,
        draggable: false
      });
      this.layer.add(this.templateNode);
      this.templateNode.moveToBottom();
      this.layer.batchDraw();
    };
  }

  onTextChange(text: string) {
    this.customization.customText = text;
    this.textNode.text(text);
    this.layer.batchDraw();
  }

  onFontChange(font: string) {
    this.customization.font = font;
    this.textNode.fontFamily(font);
    this.layer.batchDraw();
  }

  onSizeChange(size: number) {
    this.customization.fontSize = size;
    this.textNode.fontSize(size);
    this.layer.batchDraw();
  }

  onColorChange(color: string) {
    this.customization.color = color;
    this.textNode.fill(color);
    this.appliedColor = color; // 👈 Update preview swatch

    this.layer.batchDraw();
  }

  bringTextToFront() {
    this.textNode.moveToTop();
    this.layer.batchDraw();
  }
  sendTextToBack() {
    this.textNode.moveToBottom();
    this.textNode.moveUp(); // ensures text is above template, not behind
    this.layer.batchDraw();
  }
  bringImageToFront() {
    if (this.uploadedImageNode) {
      this.uploadedImageNode.moveToTop();
      this.layer.batchDraw();
    }
  }
  sendImageToBack() {
    if (this.uploadedImageNode) {
      this.uploadedImageNode.moveToBottom();
      this.uploadedImageNode.moveUp();
      this.layer.batchDraw();
    }
  }
  onTextRotationChange(evt: Event) {
    const deg = +(evt.target as HTMLInputElement).value;
    this.textNode.rotation(deg);
    this.layer.batchDraw();
  }
  onImageRotationChange(evt: Event) {
    if (this.uploadedImageNode) {
      const deg = +(evt.target as HTMLInputElement).value;
      this.uploadedImageNode.rotation(deg);
      this.layer.batchDraw();
    }
  }

  onFileSelected(evt: Event) {
    const inp = evt.target as HTMLInputElement;
    if (!inp.files?.length) return;
    const file = inp.files[0];
    
    // Check file size (limit to 5MB to prevent issues)
    if (file.size > 5 * 1024 * 1024) {
      this.toastSvc.show('Image size too large. Please choose an image under 5MB.', 'error');
      inp.value = ''; // Reset the input
      return;
    }
    
    this.customization.uploadedImageFile = file;

    const reader = new FileReader();
    reader.onload = () => {
      const imgObj = new window.Image();
      imgObj.src   = reader.result as string;
      imgObj.onload = () => {
        // Remove previous image and transformer if they exist
        this.uploadedImageNode?.destroy();
        this.imageTransformer?.destroy();

        const width = this.customization.imageWidth || 100;
        const height = this.customization.imageHeight || 100;

        this.uploadedImageNode = new Konva.Image({
          image:     imgObj,
          x:         this.customization.imagePosition.x,
          y:         this.customization.imagePosition.y,
          width,
          height,
          draggable: true
        });
        this.uploadedImageNode.on('dragend', () =>
          this.customization.imagePosition = this.uploadedImageNode!.position()
        );

        this.uploadedImageNode.on('click', () => {
          this.imageSelected = true;
          this.showImageTransformer();
        });

        this.layer.add(this.uploadedImageNode!);
        this.layer.draw();

        this.customization.uploadedImagePath = reader.result as string;
        this.customization.imageWidth = width;
        this.customization.imageHeight = height;

        this.uploadedImageNode.on('transformend', () => {
          this.customization.imageWidth = this.uploadedImageNode!.width() * this.uploadedImageNode!.scaleX();
          this.customization.imageHeight = this.uploadedImageNode!.height() * this.uploadedImageNode!.scaleY();
          this.uploadedImageNode!.width(this.customization.imageWidth);
          this.uploadedImageNode!.height(this.customization.imageHeight);
          this.uploadedImageNode!.scaleX(1);
          this.uploadedImageNode!.scaleY(1);
          this.layer.batchDraw();
        });
      };
    };
    reader.onerror = () => {
      this.toastSvc.show('Error loading image. Please try another file.', 'error');
      inp.value = ''; // Reset the input
    };
    reader.readAsDataURL(file);
  }

  private showImageTransformer() {
    if (!this.uploadedImageNode) return;
    this.imageTransformer?.destroy();
    this.imageTransformer = new Konva.Transformer({
      nodes: [this.uploadedImageNode],
      enabledAnchors: ['top-left', 'top-right', 'bottom-left', 'bottom-right'],
      boundBoxFunc: (oldBox, newBox) => {
        if (newBox.width < 40 || newBox.height < 40) {
          return oldBox;
        }
        return newBox;
      }
    });
    this.layer.add(this.imageTransformer);
    this.layer.draw();
  }

  private hideImageTransformer() {
    this.imageTransformer?.destroy();
    this.imageTransformer = undefined;
    this.layer.draw();
  }

  onSave() {
    this.customization.textPosition = this.textNode.position();
    if (this.uploadedImageNode) {
      this.customization.imagePosition = this.uploadedImageNode.position();
      this.customization.imageWidth = this.uploadedImageNode.width();
      this.customization.imageHeight = this.uploadedImageNode.height();
    }
    
    // Create snapshot
    const dataUrl = this.stage.toDataURL({ mimeType: 'image/png' });
    fetch(dataUrl)
      .then(res => res.blob())
      .then(blob => {
        this.customization.snapshotFile = new File(
          [blob],
          `custom_snapshot_${Date.now()}.png`,
          { type: 'image/png' }
        );
        
        // Store only text data in localStorage to avoid quota issues
        const textOnlyData = {
          template: this.customization.template,
          customText: this.customization.customText,
          font: this.customization.font,
          fontSize: this.customization.fontSize,
          color: this.customization.color,
          textPosition: this.customization.textPosition,
          imagePosition: this.customization.imagePosition,
          imageWidth: this.customization.imageWidth,
          imageHeight: this.customization.imageHeight
        };
        
        try {
          localStorage.setItem(
            `customization_${this.product.id}`,
            JSON.stringify(textOnlyData)
          );
        } catch (e) {
          console.warn('Could not save to localStorage, but customization will still work:', e);
        }
        
        this.save.emit({ ...this.customization });
        this.close.emit();
        this.toastSvc.show(
          `Product customization saved!`,
          this.product.imageUrl
        );
      })
      .catch(error => {
        console.error('Error creating snapshot:', error);
        this.toastSvc.show('Error saving customization. Please try again.', 'error');
      });
  }

  onClose() {
    this.close.emit();
  }
}