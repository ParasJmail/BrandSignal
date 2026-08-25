import amqp from 'amqplib';
import dotenv from 'dotenv';
import { scrapeCitations } from './scraper.js';

dotenv.config();

const RABBITMQ_URL = process.env.RABBITMQ_URL || 'amqp://localhost:5672';
const INPUT_QUEUE = process.env.INPUT_QUEUE || 'campaign_audit_queue';
const OUTPUT_QUEUE = process.env.OUTPUT_QUEUE || 'audit_results_queue';

async function startScraperService() {
  try {
    console.log('[Node Scraper] Connecting to RabbitMQ...');
    const connection = await amqp.connect(RABBITMQ_URL);
    const channel = await connection.createChannel();

    // 1. Match the exact queue arguments configured in .NET
    const mainQueueOptions = {
      durable: true,
      deadLetterExchange: 'campaign_audit_dlx',
      deadLetterRoutingKey: 'campaign.audit.deadletter'
    };

    await channel.assertQueue(INPUT_QUEUE, mainQueueOptions);
    await channel.assertQueue(OUTPUT_QUEUE, { durable: true });

    channel.prefetch(1);
    console.log(`[Node Scraper] Listening on queue: '${INPUT_QUEUE}'...`);

    channel.consume(INPUT_QUEUE, async (msg) => {
      if (!msg) return;

      const rawPayload = msg.content.toString();
      console.log(`\n[Node Scraper] Received job: ${rawPayload}`);

      try {
        const data = JSON.parse(rawPayload);
        const { CampaignId, CompanyName, TargetKeyword } = data;

        // 1. Scrape citations
        const citations = await scrapeCitations(CompanyName, TargetKeyword);

        // 2. Build enriched result payload
        const resultPayload = {
          CampaignId: CampaignId,
          CompanyName: CompanyName,
          TargetKeyword: TargetKeyword,
          SentimentScore: Math.floor(Math.random() * 25) + 75,
          BrandPositioning: `${CompanyName} has search presence across ${citations.length} result pages for '${TargetKeyword}'.`,
          Summary: `Scraped citations successfully. Top result: "${citations[0] || TargetKeyword}".`,
          TopCompetitors: [`${TargetKeyword} Competitor A`, `${TargetKeyword} Competitor B`],
          RecommendedKeywords: [`${TargetKeyword} reviews`, `best ${TargetKeyword} tools`],
          ScrapedCitations: citations,
          ProcessedAt: new Date().toISOString()
        };

        // 3. Publish to output queue
        channel.sendToQueue(
          OUTPUT_QUEUE,
          Buffer.from(JSON.stringify(resultPayload)),
          { persistent: true }
        );

        console.log(`[Node Scraper] Published scraped results for Campaign ID ${CampaignId} to '${OUTPUT_QUEUE}'.`);
        channel.ack(msg);
      } catch (err) {
        console.error('[Node Scraper Error] Failed to process job:', err.message);
        channel.nack(msg, false, false);
      }
    });

  } catch (error) {
    console.error('[Node Scraper Error] Connection error:', error.message);
    setTimeout(startScraperService, 5000);
  }
}

startScraperService();