import axios from 'axios';
import * as cheerio from 'cheerio';

/**
 * Scrapes search result citations for a given company name and keyword.
 */
export async function scrapeCitations(companyName, targetKeyword){
    try{
       console.log(`[Scraper] Fetching citations for "${companyName}" with keyword "${targetKeyword}"...`);
       
       const searchUrl = `https://html.duckduckgo.com/html/?q=${encodeURIComponent(companyName + ' ' + targetKeyword)}`;

       const response = await axios.get(searchUrl, {
        headers: {
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
        },
        timeout: 5000
       });

       const $ = cheerio.load(response.data);
        const titles = [];

        $('.result__title').each((i, el) => {
        if (i < 5) {
            titles.push($(el).text().trim());
        }
        });

        console.log(`[Scraper] Found ${titles.length} citations.`);
        return titles;
    } catch (error) {
        console.warn(`[Scraper Warning] Fallback citations used: ${error.message}`);
        return [
        `${companyName} features and overview`,
        `Top alternatives to ${companyName} for ${targetKeyword}`,
        `Best ${targetKeyword} software solutions`
        ];
    }
}