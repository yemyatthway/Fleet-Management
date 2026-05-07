const encoder = new TextEncoder();

const escapeXml = (value) =>
  String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");

const escapePdf = (value) =>
  String(value ?? "")
    .replace(/\\/g, "\\\\")
    .replace(/\(/g, "\\(")
    .replace(/\)/g, "\\)");

const normalizePdfText = (value) =>
  String(value ?? "")
    .replace(/[–—]/g, "-")
    .replace(/[•]/g, "-")
    .replace(/[^\x09\x0a\x0d\x20-\x7e]/g, "?");

const downloadBlob = (blob, fileName) => {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
};

const crcTable = Array.from({ length: 256 }, (_, index) => {
  let c = index;
  for (let k = 0; k < 8; k += 1) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});

const crc32 = (bytes) => {
  let crc = 0xffffffff;
  for (const byte of bytes) crc = crcTable[(crc ^ byte) & 0xff] ^ (crc >>> 8);
  return (crc ^ 0xffffffff) >>> 0;
};

const uint16 = (value) => [value & 0xff, (value >>> 8) & 0xff];
const uint32 = (value) => [
  value & 0xff,
  (value >>> 8) & 0xff,
  (value >>> 16) & 0xff,
  (value >>> 24) & 0xff,
];
const columnName = (index) => {
  let name = "";
  let value = index + 1;
  while (value > 0) {
    const remainder = (value - 1) % 26;
    name = String.fromCharCode(65 + remainder) + name;
    value = Math.floor((value - 1) / 26);
  }
  return name;
};

const sanitizeSheetName = (value) => {
  const cleaned = String(value || "Report")
    .replace(/[\[\]:*?/\\]/g, "-")
    .replace(/^'+|'+$/g, "")
    .trim()
    .slice(0, 31);

  return cleaned || "Report";
};

const shouldExportNumber = (key, value) => {
  if (value === null || value === undefined || value === "") return false;
  if (typeof value === "number") return Number.isFinite(value);
  if (
    !/(amount|cost|total|price|value|stock|quantity|weight|volume|distance|odometer|liters)$/i.test(
      key,
    )
  )
    return false;
  return Number.isFinite(Number(value));
};

const createZip = (files) => {
  const chunks = [];
  const centralDirectory = [];
  let offset = 0;

  files.forEach((file) => {
    const nameBytes = encoder.encode(file.name);
    const dataBytes = encoder.encode(file.content);
    const crc = crc32(dataBytes);
    const localHeader = new Uint8Array([
      ...uint32(0x04034b50),
      ...uint16(20),
      ...uint16(0),
      ...uint16(0),
      ...uint16(0),
      ...uint16(0),
      ...uint32(crc),
      ...uint32(dataBytes.length),
      ...uint32(dataBytes.length),
      ...uint16(nameBytes.length),
      ...uint16(0),
    ]);

    chunks.push(localHeader, nameBytes, dataBytes);
    centralDirectory.push({ file, nameBytes, dataBytes, crc, offset });
    offset += localHeader.length + nameBytes.length + dataBytes.length;
  });

  const centralStart = offset;
  centralDirectory.forEach(
    ({ file, nameBytes, dataBytes, crc, offset: localOffset }) => {
      const header = new Uint8Array([
        ...uint32(0x02014b50),
        ...uint16(20),
        ...uint16(20),
        ...uint16(0),
        ...uint16(0),
        ...uint16(0),
        ...uint16(0),
        ...uint32(crc),
        ...uint32(dataBytes.length),
        ...uint32(dataBytes.length),
        ...uint16(nameBytes.length),
        ...uint16(0),
        ...uint16(0),
        ...uint16(0),
        ...uint16(0),
        ...uint32(0),
        ...uint32(localOffset),
      ]);
      chunks.push(header, nameBytes);
      offset += header.length + nameBytes.length;
    },
  );

  const centralSize = offset - centralStart;
  chunks.push(
    new Uint8Array([
      ...uint32(0x06054b50),
      ...uint16(0),
      ...uint16(0),
      ...uint16(files.length),
      ...uint16(files.length),
      ...uint32(centralSize),
      ...uint32(centralStart),
      ...uint16(0),
    ]),
  );

  return new Blob(chunks, { type: "application/zip" });
};

export const exportRowsToXlsx = ({
  fileName,
  sheetName,
  columns,
  rows,
  formatCell,
}) => {
  const safeSheetName = sanitizeSheetName(sheetName);
  const headerCells = columns
    .map(
      (column, index) =>
        `<c r="${columnName(index)}1" t="inlineStr"><is><t>${escapeXml(column.label)}</t></is></c>`,
    )
    .join("");
  const bodyRows = rows
    .map((row, rowIndex) => {
      const rowNumber = rowIndex + 2;
      const cells = columns
        .map((column, columnIndex) => {
          const rawValue = row[column.key];
          const ref = `${columnName(columnIndex)}${rowNumber}`;
          if (shouldExportNumber(column.key, rawValue)) {
            return `<c r="${ref}"><v>${Number(rawValue)}</v></c>`;
          }
          const value = formatCell
            ? formatCell(rawValue, column.key)
            : rawValue;
          return `<c r="${ref}" t="inlineStr"><is><t>${escapeXml(value)}</t></is></c>`;
        })
        .join("");
      return `<row r="${rowNumber}">${cells}</row>`;
    })
    .join("");

  const worksheet = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1">${headerCells}</row>${bodyRows}</sheetData></worksheet>`;
  const workbook = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="${escapeXml(safeSheetName)}" sheetId="1" r:id="rId1"/></sheets></workbook>`;
  const workbookRels = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>`;
  const rootRels = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>`;
  const contentTypes = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>`;

  const blob = createZip([
    { name: "[Content_Types].xml", content: contentTypes },
    { name: "_rels/.rels", content: rootRels },
    { name: "xl/workbook.xml", content: workbook },
    { name: "xl/_rels/workbook.xml.rels", content: workbookRels },
    { name: "xl/worksheets/sheet1.xml", content: worksheet },
  ]);
  downloadBlob(
    new Blob([blob], {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    }),
    fileName,
  );
};

export const exportRowsToPdf = ({
  fileName,
  title,
  subtitle = "FleetManager report",
  columns,
  rows,
  formatCell,
  filters = {},
}) => {
  const pageWidth = 842;
  const pageHeight = 595;
  const margin = 32;
  const tableX = margin;
  const tableWidth = pageWidth - margin * 2;
  const tableTop = 152;
  const rowHeight = 26;
  const footerY = 24;
  const colWidth = tableWidth / Math.max(columns.length, 1);
  const rowsPerPage = Math.max(
    1,
    Math.floor((pageHeight - tableTop - 58) / rowHeight),
  );
  const pageCount = Math.max(1, Math.ceil(rows.length / rowsPerPage));
  const generatedAt = new Date().toLocaleString("en-US", {
    timeZone: "Asia/Yangon",
  });
  const activeFilters = Object.entries(filters)
    .filter(([, value]) => value)
    .map(([key, value]) => `${key}: ${value}`)
    .join("   ");

  const trimCell = (value, width, fontSize = 9) => {
    const text = normalizePdfText(value || "-");
    const maxChars = Math.max(6, Math.floor(width / (fontSize * 0.52)));
    return text.length > maxChars
      ? `${text.slice(0, Math.max(1, maxChars - 1))}...`
      : text;
  };

  const text = (
    content,
    x,
    y,
    size = 9,
    font = "F1",
    color = "0.09 0.12 0.20",
  ) =>
    [
      "BT",
      `${color} rg`,
      `/${font} ${size} Tf`,
      `${x.toFixed(2)} ${y.toFixed(2)} Td`,
      `(${escapePdf(normalizePdfText(content))}) Tj`,
      "ET",
    ].join("\n");

  const rect = (x, y, width, height, color) =>
    `${color} rg\n${x.toFixed(2)} ${y.toFixed(2)} ${width.toFixed(2)} ${height.toFixed(2)} re f`;

  const line = (x1, y1, x2, y2, color = "0.88 0.91 0.95", width = 0.6) =>
    `${color} RG\n${width} w\n${x1.toFixed(2)} ${y1.toFixed(2)} m ${x2.toFixed(2)} ${y2.toFixed(2)} l S`;

  const buildPageStream = (pageIndex) => {
    const pageRows = rows.slice(
      pageIndex * rowsPerPage,
      pageIndex * rowsPerPage + rowsPerPage,
    );
    const chunks = [];

    chunks.push(rect(0, pageHeight - 82, pageWidth, 82, "0.96 0.98 1"));
    chunks.push(rect(0, pageHeight - 82, 7, 82, "0.15 0.39 0.92"));
    chunks.push(text("FleetManager", margin, pageHeight - 36, 20, "F2"));
    chunks.push(
      text(subtitle, margin, pageHeight - 58, 10, "F1", "0.39 0.46 0.57"),
    );
    chunks.push(
      text(
        title,
        pageWidth - margin - 260,
        pageHeight - 36,
        16,
        "F2",
        "0.15 0.39 0.92",
      ),
    );
    chunks.push(
      text(
        `Generated: ${generatedAt} MMT`,
        pageWidth - margin - 260,
        pageHeight - 58,
        9,
        "F1",
        "0.39 0.46 0.57",
      ),
    );

    chunks.push(rect(margin, pageHeight - 124, tableWidth, 28, "0.98 0.99 1"));
    chunks.push(
      line(margin, pageHeight - 124, margin + tableWidth, pageHeight - 124),
    );
    chunks.push(
      line(margin, pageHeight - 96, margin + tableWidth, pageHeight - 96),
    );
    chunks.push(
      text(`Records: ${rows.length}`, margin + 12, pageHeight - 114, 10, "F2"),
    );
    chunks.push(
      text(
        activeFilters || "Filters: All records",
        margin + 130,
        pageHeight - 114,
        9,
        "F1",
        "0.39 0.46 0.57",
      ),
    );

    chunks.push(
      rect(tableX, pageHeight - tableTop, tableWidth, 28, "0.15 0.39 0.92"),
    );
    columns.forEach((column, index) => {
      chunks.push(
        text(
          trimCell(column.label, colWidth - 12, 8),
          tableX + index * colWidth + 7,
          pageHeight - tableTop + 10,
          8,
          "F2",
          "1 1 1",
        ),
      );
      if (index > 0)
        chunks.push(
          line(
            tableX + index * colWidth,
            pageHeight - tableTop,
            tableX + index * colWidth,
            pageHeight - tableTop + 28,
            "0.43 0.61 0.95",
            0.4,
          ),
        );
    });

    pageRows.forEach((row, rowIndex) => {
      const y = pageHeight - tableTop - rowHeight * (rowIndex + 1);
      if (rowIndex % 2 === 0)
        chunks.push(rect(tableX, y, tableWidth, rowHeight, "0.98 0.99 1"));
      chunks.push(
        line(tableX, y, tableX + tableWidth, y, "0.90 0.93 0.96", 0.45),
      );
      columns.forEach((column, columnIndex) => {
        const value = formatCell
          ? formatCell(row[column.key], column.key)
          : row[column.key];
        chunks.push(
          text(
            trimCell(value, colWidth - 12),
            tableX + columnIndex * colWidth + 7,
            y + 9,
            8.5,
            "F1",
          ),
        );
      });
    });

    if (!rows.length) {
      chunks.push(
        text(
          "No records found for this report.",
          tableX + 12,
          pageHeight - tableTop - 42,
          11,
          "F1",
          "0.39 0.46 0.57",
        ),
      );
    }

    chunks.push(line(margin, footerY + 16, pageWidth - margin, footerY + 16));
    chunks.push(
      text(
        "FleetManager Report Export",
        margin,
        footerY,
        8,
        "F1",
        "0.39 0.46 0.57",
      ),
    );
    chunks.push(
      text(
        `Page ${pageIndex + 1} of ${pageCount}`,
        pageWidth - margin - 70,
        footerY,
        8,
        "F1",
        "0.39 0.46 0.57",
      ),
    );
    return chunks.join("\n");
  };

  const streams = Array.from({ length: pageCount }, (_, index) =>
    buildPageStream(index),
  );
  const pageObjectStart = 5;
  const contentObjectStart = pageObjectStart + pageCount;
  const kids = streams
    .map((_, index) => `${pageObjectStart + index} 0 R`)
    .join(" ");
  const objects = [
    "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
    `2 0 obj << /Type /Pages /Kids [${kids}] /Count ${pageCount} >> endobj`,
    "3 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
    "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj",
    ...streams.map(
      (_, index) =>
        `${pageObjectStart + index} 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageWidth} ${pageHeight}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents ${contentObjectStart + index} 0 R >> endobj`,
    ),
    ...streams.map(
      (stream, index) =>
        `${contentObjectStart + index} 0 obj << /Length ${encoder.encode(stream).length} >> stream\n${stream}\nendstream endobj`,
    ),
  ];

  let pdf = "%PDF-1.4\n";
  const offsets = [0];
  objects.forEach((object) => {
    offsets.push(pdf.length);
    pdf += `${object}\n`;
  });
  const xrefOffset = pdf.length;
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  offsets.slice(1).forEach((offset) => {
    pdf += `${String(offset).padStart(10, "0")} 00000 n \n`;
  });
  pdf += `trailer << /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF`;
  downloadBlob(new Blob([pdf], { type: "application/pdf" }), fileName);
};
