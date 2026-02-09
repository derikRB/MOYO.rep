import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

export function addBranding(doc: jsPDF, reportTitle: string, date: string, userName: string) {
  // Logo
  doc.addImage('https://i.postimg.cc/J7JL3WKC/logo.jpg', 'JPEG', 15, 15, 36, 36);

  // Report Title
  doc.setTextColor(255, 91, 170); // Sego & Bux Pink
  doc.setFontSize(22);
  doc.text(reportTitle, 60, 28);

  // Metadata
  doc.setFontSize(12);
  doc.setTextColor(100, 10, 50); // Subtle magenta
  doc.text(`Generated: ${date}`, 60, 36);

  doc.setTextColor(165, 0, 87); // Darker magenta for User
  doc.text(`User: ${userName}`, 60, 44);

  // Optional: Company Tagline
  doc.setFontSize(10);
  doc.setTextColor(90, 90, 90);

  // Section line
  doc.setDrawColor(255, 91, 170);
  doc.setLineWidth(0.8);
  doc.line(15, 52, 195, 52);

  // Reset color for next content
  doc.setTextColor(0,0,0);
}

export { jsPDF, autoTable };
