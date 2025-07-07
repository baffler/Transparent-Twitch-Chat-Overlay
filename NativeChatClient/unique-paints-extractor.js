// Unique Paint Types Extractor
// This script fetches paint data from a GraphQL endpoint and extracts unique paint types

// Configuration - Update these values
const GRAPHQL_ENDPOINT = 'https://7tv.io/v3/gql';

/**
 * Main function to run the script
 */
async function main() {
  try {
    console.log("Fetching paint data from GraphQL...");
    const queryResults = await fetchPaintData();
    
    console.log("Processing results to extract unique paint types...");
    const filteredResults = filterUniquePaintTypes(queryResults);
    
    if (filteredResults.error) {
      console.error("Error:", filteredResults.error);
      return;
    }
    
    // Example of what you might do with the results
    console.log("\n=== RESULTS SUMMARY ===");
    console.log(`Original paints count: ${getOriginalPaintsCount(queryResults)}`);
    console.log(`Unique paint types: ${filteredResults.uniqueTypesReport.length}`);
    
    // Optionally save to file
    saveResultsToFile(filteredResults, 'unique-paint-types.json');
    
  } catch (error) {
    console.error("Script failed:", error);
  }
}

/**
 * Get the count of paints from the original query results
 */
function getOriginalPaintsCount(queryResults) {
  if (queryResults.cosmetics && queryResults.cosmetics.paints) {
    return queryResults.cosmetics.paints.length;
  } else if (queryResults.data && queryResults.data.cosmetics && queryResults.data.cosmetics.paints) {
    return queryResults.data.cosmetics.paints.length;
  }
  return 'unknown';
}

/**
 * Fetch paint data from GraphQL endpoint
 */
async function fetchPaintData() {
  const query = `
    query MyQuery {
      cosmetics {
        paints {
          angle
          function
          id
          image_url
          name
          repeat
          shadows {
            color
            radius
            x_offset
            y_offset
          }
          shape
          stops {
            at
            center_at
            color
          }
        }
      }
    }
  `;

  const headers = {
    'Content-Type': 'application/json',
  };

  const response = await fetch(GRAPHQL_ENDPOINT, {
    method: 'POST',
    headers: headers,
    body: JSON.stringify({ query }),
  });

  if (!response.ok) {
    throw new Error(`GraphQL request failed with status ${response.status}: ${response.statusText}`);
  }

  return await response.json();
}

/**
 * Filter query results to keep only unique paint types
 */
function filterUniquePaintTypes(queryResults) {
  // Handle different possible structures of query results
  let paints = [];
  
  // Check various possible structures for the paint data
  if (queryResults.cosmetics && queryResults.cosmetics.paints) {
    paints = queryResults.cosmetics.paints;
  } else if (queryResults.data && queryResults.data.cosmetics && queryResults.data.cosmetics.paints) {
    paints = queryResults.data.cosmetics.paints;
  } else {
    console.error("Could not find paints array in the query results. Please check the structure.");
    console.log("Query results structure:", JSON.stringify(queryResults, null, 2));
    return { error: "Invalid query results structure", queryResults };
  }
  
  console.log(`Found ${paints.length} total paints to process`);
  
  // Create a Map to track unique paint types
  const uniqueTypes = new Map();
  const uniquePaints = [];
  
  // Function to generate a unique key for each paint type
  const getPaintTypeKey = (paint) => {
    // Key components include function type and shadow presence
    const hasShadow = paint.shadows && paint.shadows.length > 0;
    const functionType = paint.function || 'unknown';
    
    // Start building the key
    let key = `${functionType}`;
    
    // Add shadow information
    key += `_shadow:${hasShadow}`;
    
    // Add repeat information for all paints (especially important for URL-based ones)
    const repeatValue = paint.repeat || 'none';
    key += `_repeat:${repeatValue}`;
    
    // For gradients, also consider shape and stop count
    if (functionType.includes('gradient')) {
      const shape = paint.shape || 'none';
      const stopCount = paint.stops ? paint.stops.length : 0;
      key += `_shape:${shape}_stops:${stopCount}`;
      
      // For linear gradients, angle is important
      if (functionType === 'linear-gradient' && paint.angle !== undefined) {
        // Group angles into ranges to avoid too many unique types
        const angleRange = Math.floor(paint.angle / 45) * 45;
        key += `_angle:${angleRange}`;
      }
    }
    
    return key;
  };
  
  // Process each paint in the results
  for (const paint of paints) {
    const typeKey = getPaintTypeKey(paint);
    
    // If we haven't seen this type before, add it to our results
    if (!uniqueTypes.has(typeKey)) {
      uniqueTypes.set(typeKey, {
        paint: paint,
        description: createTypeDescription(paint)
      });
      uniquePaints.push(paint);
    }
  }
  
  // Build the filtered result object with the same structure as original
  let result;
  if (queryResults.cosmetics) {
    // Maintain the original structure
    result = {
      cosmetics: {
        paints: uniquePaints
      }
    };
  } else if (queryResults.data) {
    result = {
      data: {
        cosmetics: {
          paints: uniquePaints
        }
      }
    };
  }
  
  // Add the report to the result
  result.uniqueTypesReport = Array.from(uniqueTypes.entries()).map(([key, value]) => ({
    key,
    description: value.description,
    paintName: value.paint.name,
    paintId: value.paint.id
  }));
  
  // Print the report of unique types
  console.log("\n=== Unique Paint Types Report ===");
  result.uniqueTypesReport.forEach((type, index) => {
    console.log(`${index + 1}. ${type.description} (ID: ${type.paintId}, Name: ${type.paintName})`);
    console.log(`   Type key: ${type.key}`);
    console.log("---");
  });
  console.log(`Total unique types found: ${result.uniqueTypesReport.length}`);
  
  return result;
}

/**
 * Create a human-readable description of the paint type
 */
function createTypeDescription(paint) {
  const functionType = paint.function || "unknown";
  const hasShadow = paint.shadows && paint.shadows.length > 0;
  let description = "";
  
  // Base type description
  if (functionType === "url") {
    description = "URL-based image";
  } else if (functionType === "linear-gradient") {
    description = "Linear gradient";
    if (paint.stops) {
      description += ` with ${paint.stops.length} color stops`;
    }
    if (paint.angle !== undefined) {
      description += `, angle: ${paint.angle}°`;
    }
  } else if (functionType === "radial-gradient") {
    description = "Radial gradient";
    if (paint.shape) {
      description += ` (${paint.shape})`;
    }
    if (paint.stops) {
      description += ` with ${paint.stops.length} color stops`;
    }
  } else if (functionType === "conic-gradient") {
    description = "Conic gradient";
    if (paint.stops) {
      description += ` with ${paint.stops.length} color stops`;
    }
  } else {
    description = functionType;
  }
  
  // Add shadow information
  if (hasShadow) {
    description += " with shadow";
  } else {
    description += " without shadow";
  }
  
  // Add repeat information for all paints (not just URL-based ones)
  if (paint.repeat) {
    description += `, repeat: ${paint.repeat}`;
  } else {
    description += ", no repeat";
  }
  
  return description;
}

/**
 * Save results to a JSON file
 */
function saveResultsToFile(data, filename) {
  try {
    // In browser, we'll create a download link
    if (typeof window !== 'undefined') {
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      
      // Cleanup
      setTimeout(() => {
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }, 0);
      
      console.log(`Results ready for download as ${filename}`);
    } 
    // In Node.js environment, we'd use fs.writeFileSync
    else if (typeof process !== 'undefined' && process.versions && process.versions.node) {
      const fs = require('fs');
      fs.writeFileSync(filename, JSON.stringify(data, null, 2));
      console.log(`Results saved to ${filename}`);
    }
    else {
      console.log("Results couldn't be saved to file in this environment");
      console.log("Data:", JSON.stringify(data, null, 2));
    }
  } catch (error) {
    console.error("Error saving results:", error);
  }
}

// Run the script
main().catch(console.error);
