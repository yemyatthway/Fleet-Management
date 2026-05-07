const numberPattern = /(\d+(?:\.\d+)?)/i

const parseNumber = (value) => {
  const match = String(value || '').match(numberPattern)
  return match ? Number(match[1]) : null
}

export const parseVehicleCapacity = (capacity) => {
  const text = String(capacity || '').toLowerCase()
  if (!text.trim()) return { weightKg: null, volumeM3: null }

  const tonsMatch = text.match(/(\d+(?:\.\d+)?)\s*(?:tons?|tonnes?|t)\b/i)
  const kgMatch = text.match(/(\d+(?:\.\d+)?)\s*(?:kg|kgs|kilograms?)\b/i)
  const volumeMatch = text.match(/(\d+(?:\.\d+)?)\s*(?:m3|m³|cbm|cubic\s*meters?)\b/i)

  return {
    weightKg: tonsMatch ? Number(tonsMatch[1]) * 1000 : kgMatch ? Number(kgMatch[1]) : parseNumber(text),
    volumeM3: volumeMatch ? Number(volumeMatch[1]) : null
  }
}

export const validateTripLoadAgainstVehicle = (trip, vehicle) => {
  if (!vehicle) return 'Selected vehicle could not be found.'

  const capacity = parseVehicleCapacity(vehicle.capacity)
  const loadWeightKg = Number(trip.loadWeightKg || 0)
  const loadVolumeM3 = Number(trip.loadVolumeM3 || 0)

  if (capacity.weightKg !== null && loadWeightKg > capacity.weightKg) {
    return `Load weight ${loadWeightKg.toLocaleString()} kg is higher than ${vehicle.id}'s capacity (${vehicle.capacity}). Choose another vehicle.`
  }

  if (capacity.volumeM3 !== null && loadVolumeM3 > capacity.volumeM3) {
    return `Load volume ${loadVolumeM3.toLocaleString()} m3 is higher than ${vehicle.id}'s capacity (${vehicle.capacity}). Choose another vehicle.`
  }

  return ''
}
