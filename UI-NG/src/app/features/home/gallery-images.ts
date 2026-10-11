/**
 * Slides for the home page Galleria. Files live in public/images/galleria.
 * Replaces the old ImageService + assets/data/image-addresses.json: the list is static,
 * so a typed constant needs no HTTP call, subscription or error handling.
 */
export interface GalleryImage {
  readonly src: string;
  readonly alt: string;
}

const FILE_NAMES = [
  '1LE',
  '1LE2',
  '1LE3',
  'AllertonLn@AlysburyWay',
  'Bennington',
  'BenningtonB',
  'BenningtonC',
  'ChattahoocheRiveClubTennis',
  'ColwynCt',
  'DavesCreekDr@DavesCreekRd',
  'DavisLoveDr@LandsdowneCt',
  'JamesCreek',
  'Me',
  'RiverClubDr',
  'SCCA@AtlantaMotorSpeedway',
  'Trammel',
  'WindermereDr@StratfordAptFacingWest',
  'WindermereGolfClub@StanfordDr',
] as const;

/** "DavesCreekDr@DavesCreekRd" -> "Daves Creek Dr at Daves Creek Rd" */
function toAltText(fileName: string): string {
  return fileName
    .replace(/@/g, ' at ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim();
}

export const GALLERY_IMAGES: readonly GalleryImage[] = FILE_NAMES.map((name) => ({
  src: `images/galleria/${encodeURIComponent(name)}.jpg`,
  alt: toAltText(name),
}));
