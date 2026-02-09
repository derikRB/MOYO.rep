import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatbotService } from '../services/chatbot.service';
import { ChatbotConfigService } from '../services/chatbot-config.service';

@Component({
  selector: 'app-chatbot-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot-widget.component.html',
  styleUrls: ['./chatbot-widget.component.scss']
})
export class ChatbotWidgetComponent implements OnInit {
  messages: { text: string; from: 'user' | 'bot' }[] = [];
  input = '';
  showWidget = false;
  config: any;

  constructor(private bot: ChatbotService, private cfg: ChatbotConfigService) {}

  ngOnInit() {
    this.cfg.getConfig().subscribe(c => this.config = c);
  }

  send() {
    if (!this.input.trim()) return;

    this.messages.push({ text: this.input, from: 'user' });

    this.bot.sendMessage(this.input).subscribe({
      next: res => this.messages.push({ text: res.reply, from: 'bot' }),
      error: () => this.messages.push({ text: "Error connecting to AI.", from: 'bot' })
    });

    this.input = '';
  }

  toggleWidget() {
    this.showWidget = !this.showWidget;
  }
}
