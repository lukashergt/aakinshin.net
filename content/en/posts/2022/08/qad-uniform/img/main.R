# Scripts ----------------------------------------------------------------------
source("utils.R")

# Settings ---------------------------------------------------------------------
settings <- list(
)

# Functions --------------------------------------------------------------------

# Data -------------------------------------------------------------------------

# Tables -----------------------------------------------------------------------

# Figures ----------------------------------------------------------------------

figure_qad <- function() {
  p <- seq(0, 1, by = 0.001)
  qad <- p / 2
  df <- data.frame(p, qad)
  ggplot(df, aes(p, qad)) +
    geom_line() +
    labs(y = "QAD(X, p)", title = "QAD of the Uniform distribution")
}

# Plotting ---------------------------------------------------------------------
regenerate_figures()
